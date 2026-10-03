namespace SendWithMailgun
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Tracks one stage of a Mailgun operation: an internal "stage:&lt;name&gt;" span and a stage duration measurement.
    /// Call <see cref="Complete"/> on success; disposing without completing records the stage as failed.
    /// Instrumentation is best-effort; no method on this class throws.
    /// </summary>
    internal sealed class MailgunStage : IDisposable
    {
        private readonly string _Operation;
        private readonly string _Stage;
        private readonly long _StartTimestamp;
        private readonly Activity _Activity;
        private bool _Succeeded = false;
        private bool _Disposed = false;

        internal MailgunStage(string operation, string stage)
        {
            _Operation = operation;
            _Stage = stage;
            _StartTimestamp = Stopwatch.GetTimestamp();

            try
            {
                _Activity = MailgunInstrumentation.Source.StartActivity(MailgunTelemetry.SpanStagePrefix + stage, ActivityKind.Internal);
                _Activity?.SetTag(MailgunTelemetry.AttributeOperation, operation);
                _Activity?.SetTag(MailgunTelemetry.AttributeStage, stage);
            }
            catch (Exception)
            {
            }
        }

        internal void Complete()
        {
            _Succeeded = true;
        }

        internal void Fail(Exception e)
        {
            _Succeeded = false;

            try
            {
                if (_Activity != null && e != null)
                {
                    _Activity.SetTag(MailgunTelemetry.AttributeErrorType, e.GetType().FullName);
                    AddExceptionEvent(_Activity, e);
                }
            }
            catch (Exception)
            {
            }
        }

        public void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;

            try
            {
                string outcome = _Succeeded ? MailgunTelemetry.OutcomeSuccess : MailgunTelemetry.OutcomeError;
                double seconds = (Stopwatch.GetTimestamp() - _StartTimestamp) / (double)Stopwatch.Frequency;

                TagList tags = new TagList();
                tags.Add(MailgunTelemetry.AttributeOperation, _Operation);
                tags.Add(MailgunTelemetry.AttributeStage, _Stage);
                tags.Add(MailgunTelemetry.AttributeStageOutcome, outcome);
                MailgunInstrumentation.StageDuration.Record(seconds, tags);

                if (_Activity != null)
                {
                    _Activity.SetTag(MailgunTelemetry.AttributeStageOutcome, outcome);
                    _Activity.SetStatus(_Succeeded ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
                    _Activity.Dispose();
                }
            }
            catch (Exception)
            {
            }
        }

        internal static void AddExceptionEvent(Activity activity, Exception e)
        {
            if (activity == null || e == null) return;

            ActivityTagsCollection tags = new ActivityTagsCollection();
            tags.Add("exception.type", e.GetType().FullName);
            tags.Add("exception.message", e.Message);
            tags.Add("exception.stacktrace", e.ToString());
            activity.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, tags));
        }
    }
}
