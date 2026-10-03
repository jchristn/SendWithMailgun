namespace SendWithMailgun
{
    using System;
    using System.Collections.Specialized;
    using System.Diagnostics;

    /// <summary>
    /// W3C trace context propagation onto outbound Mailgun requests.
    /// On .NET (Core) and .NET Standard hosts, HttpClient injects traceparent and tracestate itself from
    /// Activity.Current, so this is a no-op there.  On .NET Framework, HttpClient does not, so the headers are added here.
    /// </summary>
    internal static class MailgunTraceContext
    {
        internal static void Inject(NameValueCollection headers)
        {
#if NETFRAMEWORK
            try
            {
                Activity current = Activity.Current;
                if (headers == null || current == null || current.IdFormat != ActivityIdFormat.W3C) return;
                if (!String.IsNullOrEmpty(headers["traceparent"])) return;

                headers["traceparent"] = current.Id;
                if (!String.IsNullOrEmpty(current.TraceStateString)) headers["tracestate"] = current.TraceStateString;
            }
            catch (Exception)
            {
            }
#endif
        }
    }
}
