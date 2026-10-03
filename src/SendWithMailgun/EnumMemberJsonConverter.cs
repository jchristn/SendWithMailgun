namespace SendWithMailgun
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// JSON converter for enums whose wire values are declared with <see cref="EnumMemberAttribute"/>, e.g. "do_not_send".
    /// System.Text.Json's <see cref="JsonStringEnumConverter"/> ignores <see cref="EnumMemberAttribute"/>, so this converter is required
    /// to read Mailgun's snake_case values.
    /// Reading accepts the <see cref="EnumMemberAttribute"/> value or the member name, ignoring case, underscores, hyphens and spaces.
    /// Unrecognized strings, null, and undefined numbers map to the member named "Unknown" when the enum defines one; otherwise a <see cref="JsonException"/> is thrown.
    /// Writing emits the <see cref="EnumMemberAttribute"/> value, or the member name when no attribute is present.
    /// This type is thread-safe.
    /// </summary>
    /// <typeparam name="T">Enum type.</typeparam>
    internal class EnumMemberJsonConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        #region Private-Members

        private static readonly Dictionary<string, T> _ReadMap = new Dictionary<string, T>(StringComparer.Ordinal);
        private static readonly Dictionary<T, string> _WriteMap = new Dictionary<T, string>();
        private static readonly bool _HasFallback = false;
        private static readonly T _Fallback = default(T);

        #endregion

        #region Constructors-and-Factories

        static EnumMemberJsonConverter()
        {
            foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                T value = (T)field.GetValue(null);
                EnumMemberAttribute attr = field.GetCustomAttribute<EnumMemberAttribute>();
                string wire = (attr != null && !String.IsNullOrEmpty(attr.Value)) ? attr.Value : field.Name;

                _WriteMap[value] = wire;
                _ReadMap[Normalize(field.Name)] = value;
                _ReadMap[Normalize(wire)] = value;
            }

            T fallback;
            if (Enum.TryParse("Unknown", false, out fallback) && Enum.IsDefined(typeof(T), fallback))
            {
                _HasFallback = true;
                _Fallback = fallback;
            }
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Read a value.
        /// </summary>
        /// <param name="reader">Reader.</param>
        /// <param name="typeToConvert">Type to convert.</param>
        /// <param name="options">Serializer options.</param>
        /// <returns>Enum value.</returns>
        /// <exception cref="JsonException">The token cannot be mapped and the enum has no "Unknown" member, or the token is not a string, number or null.</exception>
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    string str = reader.GetString();
                    T value;
                    if (str != null && _ReadMap.TryGetValue(Normalize(str), out value)) return value;
                    return FallbackOrThrow("value '" + str + "'");

                case JsonTokenType.Number:
                    int num;
                    if (reader.TryGetInt32(out num))
                    {
                        T numValue = (T)Enum.ToObject(typeof(T), num);
                        if (Enum.IsDefined(typeof(T), numValue)) return numValue;
                    }
                    return FallbackOrThrow("numeric value");

                case JsonTokenType.Null:
                    return FallbackOrThrow("null");

                default:
                    throw new JsonException("Unexpected token " + reader.TokenType + " when reading " + typeof(T).Name + ".");
            }
        }

        /// <summary>
        /// Write a value.
        /// </summary>
        /// <param name="writer">Writer.</param>
        /// <param name="value">Enum value.</param>
        /// <param name="options">Serializer options.</param>
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            string wire;
            if (_WriteMap.TryGetValue(value, out wire)) writer.WriteStringValue(wire);
            else writer.WriteNumberValue(Convert.ToInt64(value));
        }

        /// <summary>
        /// Indicates that null tokens are passed to <see cref="Read"/> so they can be mapped to "Unknown".
        /// </summary>
        public override bool HandleNull
        {
            get
            {
                return true;
            }
        }

        #endregion

        #region Private-Methods

        private static T FallbackOrThrow(string what)
        {
            if (_HasFallback) return _Fallback;
            throw new JsonException("Unable to convert " + what + " to " + typeof(T).Name + ".");
        }

        private static string Normalize(string str)
        {
            return str.Replace("_", "").Replace("-", "").Replace(" ", "").ToLowerInvariant();
        }

        #endregion
    }
}
