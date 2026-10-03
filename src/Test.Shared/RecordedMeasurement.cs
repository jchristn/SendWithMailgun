namespace Test.Shared
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A single metric measurement captured by <see cref="TelemetryCapture"/>.
    /// </summary>
    public class RecordedMeasurement
    {
        /// <summary>
        /// Instrument name.
        /// </summary>
        public string Instrument { get; set; } = "";

        /// <summary>
        /// Instrument unit, or null.
        /// </summary>
        public string? Unit { get; set; } = null;

        /// <summary>
        /// Measured value.
        /// </summary>
        public double Value { get; set; } = 0;

        /// <summary>
        /// Measurement tags.
        /// </summary>
        public Dictionary<string, object?> Tags { get; set; } = new Dictionary<string, object?>(StringComparer.Ordinal);

        /// <summary>
        /// Return the tag value as a string, or null if absent.
        /// </summary>
        /// <param name="key">Tag key.</param>
        /// <returns>String value or null.</returns>
        public string? Tag(string key)
        {
            if (!Tags.TryGetValue(key, out object? val) || val == null) return null;
            return Convert.ToString(val, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Text representation for assertion messages.
        /// </summary>
        /// <returns>String.</returns>
        public override string ToString()
        {
            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, object?> kvp in Tags) parts.Add(kvp.Key + "=" + kvp.Value);
            return Instrument + "(" + Value + ") {" + String.Join(", ", parts) + "}";
        }
    }
}
