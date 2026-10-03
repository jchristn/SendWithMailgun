namespace SendWithMailgun
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Reflection;

    /// <summary>
    /// Process-wide meter, activity source, and instruments for SendWithMailgun.
    /// </summary>
    internal static class MailgunInstrumentation
    {
        internal static readonly string Version = GetVersion();

        internal static readonly ActivitySource Source = new ActivitySource(MailgunTelemetry.ActivitySourceName, Version);

        internal static readonly Meter Meter = new Meter(MailgunTelemetry.MeterName, Version);

        internal static readonly Histogram<double> OperationDuration = Meter.CreateHistogram<double>(
            MailgunTelemetry.MetricOperationDuration,
            "s",
            "Duration of Mailgun operations, end to end.");

        internal static readonly Counter<long> Operations = Meter.CreateCounter<long>(
            MailgunTelemetry.MetricOperations,
            "{operation}",
            "Completed Mailgun operations by outcome.");

        internal static readonly UpDownCounter<long> ActiveOperations = Meter.CreateUpDownCounter<long>(
            MailgunTelemetry.MetricActiveOperations,
            "{operation}",
            "Mailgun operations currently in flight.");

        internal static readonly Histogram<double> StageDuration = Meter.CreateHistogram<double>(
            MailgunTelemetry.MetricStageDuration,
            "s",
            "Duration of each stage of a Mailgun operation.");

        internal static readonly Histogram<long> SendRecipients = Meter.CreateHistogram<long>(
            MailgunTelemetry.MetricSendRecipients,
            "{recipient}",
            "Recipients (to, cc, and bcc combined) per send.");

        internal static readonly Histogram<long> SendBodySize = Meter.CreateHistogram<long>(
            MailgunTelemetry.MetricSendBodySize,
            "By",
            "UTF-8 size of the message body per send.");

        internal static readonly Counter<long> ValidationResults = Meter.CreateCounter<long>(
            MailgunTelemetry.MetricValidationResults,
            "{validation}",
            "Successful validations by Mailgun result and risk.");

        internal static readonly ObservableGauge<long> BuildInfo = Meter.CreateObservableGauge<long>(
            MailgunTelemetry.MetricBuildInfo,
            ObserveBuildInfo,
            "1",
            "Always 1; labeled with the SendWithMailgun library version.");

        internal static void EnsureInitialized()
        {
            GC.KeepAlive(BuildInfo);
        }

        internal static int CountRecipients(params string[] lines)
        {
            int count = 0;

            foreach (string line in lines)
            {
                if (String.IsNullOrEmpty(line)) continue;

                foreach (string part in line.Split(','))
                {
                    if (!String.IsNullOrWhiteSpace(part)) count++;
                }
            }

            return count;
        }

        private static IEnumerable<Measurement<long>> ObserveBuildInfo()
        {
            return new Measurement<long>[]
            {
                new Measurement<long>(1, new KeyValuePair<string, object>(MailgunTelemetry.AttributeVersion, Version))
            };
        }

        private static string GetVersion()
        {
            try
            {
                Assembly assembly = typeof(MailgunInstrumentation).Assembly;
                AssemblyInformationalVersionAttribute info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                string version = info != null ? info.InformationalVersion : assembly.GetName().Version?.ToString();
                if (String.IsNullOrEmpty(version)) return "unknown";

                int plus = version.IndexOf('+');
                return plus > 0 ? version.Substring(0, plus) : version;
            }
            catch (Exception)
            {
                return "unknown";
            }
        }
    }
}
