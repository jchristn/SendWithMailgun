namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// HTTP request captured by <see cref="MockMailgunServer"/>.
    /// </summary>
    public class RecordedRequest
    {
        /// <summary>
        /// HTTP method.
        /// </summary>
        public string Method { get; set; } = "";

        /// <summary>
        /// Absolute path of the request URL, without query.
        /// </summary>
        public string Path { get; set; } = "";

        /// <summary>
        /// Content type header, or null.
        /// </summary>
        public string? ContentType { get; set; } = null;

        /// <summary>
        /// Authorization header, or null.
        /// </summary>
        public string? Authorization { get; set; } = null;

        /// <summary>
        /// W3C traceparent header, or null.
        /// </summary>
        public string? TraceParent { get; set; } = null;

        /// <summary>
        /// Raw request body.
        /// </summary>
        public string Body { get; set; } = "";

        /// <summary>
        /// Form fields decoded from an application/x-www-form-urlencoded body.
        /// </summary>
        public Dictionary<string, string> Form { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Decode the basic authorization header into its user and password parts.
        /// </summary>
        /// <returns>Decoded "user:password" string, or null if the header is absent or not basic.</returns>
        public string? DecodeBasicAuthorization()
        {
            if (String.IsNullOrEmpty(Authorization)) return null;
            if (!Authorization.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)) return null;
            return Encoding.UTF8.GetString(Convert.FromBase64String(Authorization.Substring(6).Trim()));
        }
    }
}
