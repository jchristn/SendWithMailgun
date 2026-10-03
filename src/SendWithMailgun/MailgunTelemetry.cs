namespace SendWithMailgun
{
    /// <summary>
    /// Public telemetry contract for SendWithMailgun.
    /// The library emits metrics through a <see cref="System.Diagnostics.Metrics.Meter"/> and traces through an
    /// <see cref="System.Diagnostics.ActivitySource"/>, both named <see cref="MeterName"/> / <see cref="ActivitySourceName"/>.
    /// It takes no dependency on any exporter.  A host subscribes to these names, for example with Radiant
    /// (settings.Sources.AddMeter / AddActivitySource) or the OpenTelemetry SDK (AddMeter / AddSource).
    /// When nothing is subscribed, emission costs effectively nothing.
    /// The strings below are a stable public contract consumed by dashboards and alerts.
    /// This class is thread-safe; it holds only constants.
    /// </summary>
    public static class MailgunTelemetry
    {
        #region Source-Names

        /// <summary>
        /// Name of the meter that carries every SendWithMailgun metric.
        /// </summary>
        public const string MeterName = "SendWithMailgun";

        /// <summary>
        /// Name of the activity source that carries every SendWithMailgun span.
        /// </summary>
        public const string ActivitySourceName = "SendWithMailgun";

        #endregion

        #region Metric-Names

        /// <summary>
        /// Histogram (seconds) of end-to-end Mailgun operation duration.
        /// Labels: mailgun.operation, mailgun.outcome, server.address, server.port, http.response.status_code (when a response was received), error.type (on failure).
        /// Prometheus: sendwithmailgun_client_operation_duration_seconds.
        /// </summary>
        public const string MetricOperationDuration = "sendwithmailgun.client.operation.duration";

        /// <summary>
        /// Counter of completed Mailgun operations.
        /// Labels: same as <see cref="MetricOperationDuration"/>.
        /// Prometheus: sendwithmailgun_client_operations_total.
        /// </summary>
        public const string MetricOperations = "sendwithmailgun.client.operations";

        /// <summary>
        /// Up/down counter of Mailgun operations currently in flight.
        /// Labels: mailgun.operation, server.address, server.port.
        /// Prometheus: sendwithmailgun_client_active_operations.
        /// </summary>
        public const string MetricActiveOperations = "sendwithmailgun.client.active_operations";

        /// <summary>
        /// Histogram (seconds) of the duration of each stage of a Mailgun operation.
        /// Labels: mailgun.operation, mailgun.stage, mailgun.stage.outcome.
        /// Prometheus: sendwithmailgun_client_stage_duration_seconds.
        /// </summary>
        public const string MetricStageDuration = "sendwithmailgun.client.stage.duration";

        /// <summary>
        /// Histogram of the number of recipients (to, cc, and bcc combined) per send.
        /// Labels: mailgun.body.format.
        /// Prometheus: sendwithmailgun_send_recipients.
        /// </summary>
        public const string MetricSendRecipients = "sendwithmailgun.send.recipients";

        /// <summary>
        /// Histogram (bytes) of the UTF-8 size of the message body per send.
        /// Labels: mailgun.body.format.
        /// Prometheus: sendwithmailgun_send_body_size_bytes.
        /// </summary>
        public const string MetricSendBodySize = "sendwithmailgun.send.body.size";

        /// <summary>
        /// Counter of successful validations by Mailgun verdict.
        /// Labels: mailgun.validation.result, mailgun.validation.risk.
        /// Prometheus: sendwithmailgun_validation_results_total.
        /// </summary>
        public const string MetricValidationResults = "sendwithmailgun.validation.results";

        /// <summary>
        /// Gauge that always reports 1, labeled with the library version.
        /// Labels: sendwithmailgun.version.
        /// Prometheus: sendwithmailgun_build_info.
        /// </summary>
        public const string MetricBuildInfo = "sendwithmailgun.build.info";

        #endregion

        #region Span-Names

        /// <summary>
        /// Client span name for <see cref="MailgunSender.SendAsync"/>.
        /// </summary>
        public const string SpanSend = "mailgun send";

        /// <summary>
        /// Client span name for <see cref="MailgunValidator.ValidateAsync"/>.
        /// </summary>
        public const string SpanValidate = "mailgun validate";

        /// <summary>
        /// Prefix for internal stage spans, for example "stage:request".
        /// </summary>
        public const string SpanStagePrefix = "stage:";

        #endregion

        #region Attribute-Keys

        /// <summary>
        /// Operation label: <see cref="OperationSend"/> or <see cref="OperationValidate"/>.
        /// </summary>
        public const string AttributeOperation = "mailgun.operation";

        /// <summary>
        /// Outcome label.  One of the Outcome constants on this class.
        /// </summary>
        public const string AttributeOutcome = "mailgun.outcome";

        /// <summary>
        /// Stage label: <see cref="StageRequest"/> or <see cref="StageDeserialize"/>.
        /// </summary>
        public const string AttributeStage = "mailgun.stage";

        /// <summary>
        /// Stage outcome label: <see cref="OutcomeSuccess"/> or <see cref="OutcomeError"/>.
        /// </summary>
        public const string AttributeStageOutcome = "mailgun.stage.outcome";

        /// <summary>
        /// Body format label: <see cref="BodyFormatText"/> or <see cref="BodyFormatHtml"/>.
        /// </summary>
        public const string AttributeBodyFormat = "mailgun.body.format";

        /// <summary>
        /// Validation result label, the <see cref="ResultEnum"/> name.
        /// </summary>
        public const string AttributeValidationResult = "mailgun.validation.result";

        /// <summary>
        /// Validation risk label, the <see cref="RiskEnum"/> name.
        /// </summary>
        public const string AttributeValidationRisk = "mailgun.validation.risk";

        /// <summary>
        /// Library version label on <see cref="MetricBuildInfo"/>.
        /// </summary>
        public const string AttributeVersion = "sendwithmailgun.version";

        /// <summary>
        /// Span-only attribute: the sending domain.
        /// </summary>
        public const string AttributeDomain = "mailgun.domain";

        /// <summary>
        /// Span-only attribute: the message ID returned by Mailgun.
        /// </summary>
        public const string AttributeMessageId = "mailgun.message.id";

        /// <summary>
        /// Span-only attribute: number of recipients (to, cc, and bcc combined).
        /// </summary>
        public const string AttributeRecipientCount = "mailgun.recipient.count";

        /// <summary>
        /// OpenTelemetry semantic-convention key for the error type (exception type name, HTTP status code, or outcome).
        /// </summary>
        public const string AttributeErrorType = "error.type";

        /// <summary>
        /// OpenTelemetry semantic-convention key for the HTTP response status code.
        /// </summary>
        public const string AttributeHttpStatusCode = "http.response.status_code";

        /// <summary>
        /// OpenTelemetry semantic-convention key for the HTTP request method (span only).
        /// </summary>
        public const string AttributeHttpMethod = "http.request.method";

        /// <summary>
        /// OpenTelemetry semantic-convention key for the Mailgun API host.
        /// </summary>
        public const string AttributeServerAddress = "server.address";

        /// <summary>
        /// OpenTelemetry semantic-convention key for the Mailgun API port.
        /// </summary>
        public const string AttributeServerPort = "server.port";

        /// <summary>
        /// OpenTelemetry semantic-convention key for the full request URL (span only).  Contains no credentials.
        /// </summary>
        public const string AttributeUrlFull = "url.full";

        #endregion

        #region Attribute-Values

        /// <summary>
        /// Operation value for sending a message.
        /// </summary>
        public const string OperationSend = "send";

        /// <summary>
        /// Operation value for validating an address.
        /// </summary>
        public const string OperationValidate = "validate";

        /// <summary>
        /// Stage value for the HTTP round trip to Mailgun.
        /// </summary>
        public const string StageRequest = "request";

        /// <summary>
        /// Stage value for parsing the Mailgun response body.
        /// </summary>
        public const string StageDeserialize = "deserialize";

        /// <summary>
        /// Body format value for plain text.
        /// </summary>
        public const string BodyFormatText = "text";

        /// <summary>
        /// Body format value for HTML.
        /// </summary>
        public const string BodyFormatHtml = "html";

        /// <summary>
        /// Outcome: Mailgun returned 200 with a usable body.
        /// </summary>
        public const string OutcomeSuccess = "success";

        /// <summary>
        /// Outcome: Mailgun returned a non-200 status code.  error.type carries the status code.
        /// </summary>
        public const string OutcomeHttpError = "http_error";

        /// <summary>
        /// Outcome: Mailgun returned 200 with an empty body.
        /// </summary>
        public const string OutcomeEmptyResponse = "empty_response";

        /// <summary>
        /// Outcome: Mailgun returned 200 but the body lacked the expected content (for example, no message ID).
        /// </summary>
        public const string OutcomeMalformedResponse = "malformed_response";

        /// <summary>
        /// Outcome: the HTTP layer returned no response object.
        /// </summary>
        public const string OutcomeNoResponse = "no_response";

        /// <summary>
        /// Outcome: the caller's cancellation token was signaled.
        /// </summary>
        public const string OutcomeCancelled = "cancelled";

        /// <summary>
        /// Outcome: the operation was cancelled without the caller's token being signaled (typically an HTTP timeout).
        /// </summary>
        public const string OutcomeTimeout = "timeout";

        /// <summary>
        /// Outcome: an exception was thrown (connection failure, parse failure, and so on).  error.type carries the exception type.
        /// </summary>
        public const string OutcomeError = "error";

        #endregion
    }
}
