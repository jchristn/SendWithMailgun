using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SendWithMailgun
{
    /// <summary>
    /// Mailgun validation result.
    /// </summary>
    public class MailgunValidationResult
    {
        #region Public-Members

        /// <summary>
        /// Address.
        /// </summary>
        [JsonPropertyName("address")]
        public string Address { get; set; }

        /// <summary>
        /// Flag indicating if address is disposable.
        /// </summary>
        [JsonPropertyName("is_disposable_address")]
        public bool IsDisposable { get; set; } = false;

        /// <summary>
        /// Specifies if the address matches a typical role such as 'sales'.
        /// </summary>
        [JsonPropertyName("is_role_address")]
        public bool IsRoleAddress { get; set; } = false;

        /// <summary>
        /// List of potential reasons why a validation might be unsuccessful.
        /// </summary>
        [JsonPropertyName("reason")]
        public List<string> Reason { get; set; } = new List<string>();

        /// <summary>
        /// Result.
        /// </summary>
        [JsonPropertyName("result")]
        public ResultEnum Result { get; set; } = ResultEnum.Unknown;

        /// <summary>
        /// Risk.
        /// </summary>
        [JsonPropertyName("risk")]
        public RiskEnum Risk { get; set; } = RiskEnum.Unknown;

        /// <summary>
        /// Root address.
        /// </summary>
        [JsonPropertyName("root_address")]
        public string RootAddress { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public MailgunValidationResult()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
