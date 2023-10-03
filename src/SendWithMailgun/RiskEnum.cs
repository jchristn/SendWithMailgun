using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;

namespace SendWithMailgun
{
    /// <summary>
    /// Risk.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RiskEnum
    {
        /// <summary>
        /// High.
        /// </summary>
        [EnumMember(Value = "high")]
        High,
        /// <summary>
        /// Medium.
        /// </summary>
        [EnumMember(Value = "medium")]
        Medium,
        /// <summary>
        /// Low.
        /// </summary>
        [EnumMember(Value = "low")]
        Low,
        /// <summary>
        /// Unknown.
        /// </summary>
        [EnumMember(Value = "unknown")]
        Unknown
    }
}
