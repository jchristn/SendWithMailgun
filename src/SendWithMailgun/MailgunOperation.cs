namespace SendWithMailgun
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;

    /// <summary>
    /// Tracks one outbound Mailgun operation: a client span, an in-flight gauge, and duration/outcome metrics.
    /// Instrumentation is best-effort; no method on this class throws.
    /// </summary>
    internal sealed class MailgunOperation : IDisposable
    {
        internal Activity Activity
        {
            get
            {
                return _Activity;
            }
        }

        internal string Operation
        {
            get
            {
                return _Operation;
            }
        }

        private readonly string _Operation;
        private readonly string _ServerAddress;
        private readonly int _ServerPort;
        private readonly long _StartTimestamp;
        private readonly Activity _Activity;
        private bool _ActiveRecorded = false;
        private bool _Completed = false;
        private int? _StatusCode = null;

        internal MailgunOperation(string operation, string spanName, string url)
        {
            _Operation = operation;
            _StartTimestamp = Stopwatch.GetTimestamp();

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                _ServerAddress = uri.Host;
                _ServerPort = uri.Port;
            }
            else
            {
                _ServerAddress = "unknown";
                _ServerPort = 0;
            }

            try
            {
                MailgunInstrumentation.EnsureInitialized();

                MailgunInstrumentation.ActiveOperations.Add(
                    1,
                    new KeyValuePair<string, object>(MailgunTelemetry.AttributeOperation, _Operation),
                    new KeyValuePair<string, object>(MailgunTelemetry.AttributeServerAddress, _ServerAddress),
                    new KeyValuePair<string, object>(MailgunTelemetry.AttributeServerPort, _ServerPort));
                _ActiveRecorded = true;

                _Activity = MailgunInstrumentation.Source.StartActivity(spanName, ActivityKind.Client);
                if (_Activity != null)
                {
                    _Activity.SetTag(MailgunTelemetry.AttributeOperation, _Operation);
                    _Activity.SetTag(MailgunTelemetry.AttributeServerAddress, _ServerAddress);
                    _Activity.SetTag(MailgunTelemetry.AttributeServerPort, _ServerPort);
                    _Activity.SetTag(MailgunTelemetry.AttributeHttpMethod, "POST");
                    _Activity.SetTag(MailgunTelemetry.AttributeUrlFull, url);
                }
            }
            catch (Exception)
            {
            }
        }

        internal void SetTag(string key, object value)
        {
            try
            {
                _Activity?.SetTag(key, value);
            }
            catch (Exception)
            {
            }
        }

        internal void SetStatusCode(int statusCode)
        {
            _StatusCode = statusCode;
            SetTag(MailgunTelemetry.AttributeHttpStatusCode, statusCode);
        }

        internal MailgunStage BeginStage(string stage)
        {
            return new MailgunStage(_Operation, stage);
        }

        internal void Complete(string outcome)
        {
            string errorType = null;

            if (outcome == MailgunTelemetry.OutcomeHttpError && _StatusCode.HasValue)
                errorType = _StatusCode.Value.ToString(CultureInfo.InvariantCulture);
            else if (outcome != MailgunTelemetry.OutcomeSuccess)
                errorType = outcome;

            Record(outcome, errorType, null);
        }

        internal void Fail(Exception e, bool callerCancelled)
        {
            string outcome;

            if (e is OperationCanceledException)
                outcome = callerCancelled ? MailgunTelemetry.OutcomeCancelled : MailgunTelemetry.OutcomeTimeout;
            else
                outcome = MailgunTelemetry.OutcomeError;

            Record(outcome, e != null ? e.GetType().FullName : outcome, e);
        }

        public void Dispose()
        {
            if (!_Completed) Record(MailgunTelemetry.OutcomeError, "unknown", null);

            try
            {
                if (_ActiveRecorded)
                {
                    MailgunInstrumentation.ActiveOperations.Add(
                        -1,
                        new KeyValuePair<string, object>(MailgunTelemetry.AttributeOperation, _Operation),
                        new KeyValuePair<string, object>(MailgunTelemetry.AttributeServerAddress, _ServerAddress),
                        new KeyValuePair<string, object>(MailgunTelemetry.AttributeServerPort, _ServerPort));
                    _ActiveRecorded = false;
                }

                _Activity?.Dispose();
            }
            catch (Exception)
            {
            }
        }

        private void Record(string outcome, string errorType, Exception e)
        {
            if (_Completed) return;
            _Completed = true;

            try
            {
                double seconds = (Stopwatch.GetTimestamp() - _StartTimestamp) / (double)Stopwatch.Frequency;

                TagList tags = new TagList();
                tags.Add(MailgunTelemetry.AttributeOperation, _Operation);
                tags.Add(MailgunTelemetry.AttributeOutcome, outcome);
                tags.Add(MailgunTelemetry.AttributeServerAddress, _ServerAddress);
                tags.Add(MailgunTelemetry.AttributeServerPort, _ServerPort);
                if (_StatusCode.HasValue) tags.Add(MailgunTelemetry.AttributeHttpStatusCode, _StatusCode.Value);
                if (errorType != null) tags.Add(MailgunTelemetry.AttributeErrorType, errorType);

                MailgunInstrumentation.Operations.Add(1, tags);
                MailgunInstrumentation.OperationDuration.Record(seconds, tags);

                if (_Activity != null)
                {
                    _Activity.SetTag(MailgunTelemetry.AttributeOutcome, outcome);

                    if (errorType == null)
                    {
                        _Activity.SetStatus(ActivityStatusCode.Ok);
                    }
                    else
                    {
                        _Activity.SetTag(MailgunTelemetry.AttributeErrorType, errorType);
                        _Activity.SetStatus(ActivityStatusCode.Error, e != null ? e.Message : outcome);
                    }

                    if (e != null) MailgunStage.AddExceptionEvent(_Activity, e);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
