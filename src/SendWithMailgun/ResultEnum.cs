using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;

namespace SendWithMailgun
{
    /// <summary>
    /// Result.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ResultEnum
    {
        /// <summary>
        /// Deliverable.
        /// </summary>
        [EnumMember(Value = "deliverable")]
        Deliverable,
        /// <summary>
        /// Undeliverable.
        /// </summary>
        [EnumMember(Value = "undeliverable")]
        Undeliverable,
        /// <summary>
        /// Do not send.
        /// </summary>
        [EnumMember(Value = "do_not_send")]
        DoNotSend,
        /// <summary>
        /// Catch all.
        /// </summary>
        [EnumMember(Value = "catch_all")]
        CatchAll,
        /// <summary>
        /// Unknown.
        /// </summary>
        [EnumMember(Value = "unknown")]
        Unknown
    }
}
