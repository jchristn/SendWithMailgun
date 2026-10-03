# SendWithMailgun Telemetry

SendWithMailgun measures every call it makes to Mailgun and hands the numbers to whatever your host already runs. It emits metrics through a `System.Diagnostics.Metrics.Meter` and traces through a `System.Diagnostics.ActivitySource`, both named `SendWithMailgun`, and stops there. It never opens a connection to a telemetry backend, never picks a vendor, and takes no exporter dependency. If nothing subscribes, the instrumentation costs a few nanoseconds per call.

**The library emits, your host collects.** Everything below describes what goes on the wire and how to pick it up.

## Contents

1. [Subscribing](#subscribing)
2. [What an operator can answer](#what-an-operator-can-answer)
3. [Metrics catalog](#metrics-catalog)
4. [Label values](#label-values)
5. [Spans catalog](#spans-catalog)
6. [Trace context propagation](#trace-context-propagation)
7. [Privacy and cardinality](#privacy-and-cardinality)
8. [Recommended PromQL alerts](#recommended-promql-alerts)
9. [Suggested dashboard](#suggested-dashboard)
10. [Testing](#testing)

## Subscribing

The two names are the entire contract. They are exposed as constants on `MailgunTelemetry`:

| Constant | Value |
| --- | --- |
| `MailgunTelemetry.MeterName` | `SendWithMailgun` |
| `MailgunTelemetry.ActivitySourceName` | `SendWithMailgun` |

There are no configuration keys. Telemetry is always emitted and becomes active when a listener subscribes; the host controls sampling, export, and retention.

### Radiant

```csharp
RadiantSettings settings = new RadiantSettings("my-service");
settings.Sources.AddMeter(MailgunTelemetry.MeterName);
settings.Sources.AddActivitySource(MailgunTelemetry.ActivitySourceName);

using (RadiantHost host = RadiantHost.Start(settings))
{
    // run the app
}
```

### OpenTelemetry SDK

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m
        .AddMeter(MailgunTelemetry.MeterName)
        .AddView(MailgunTelemetry.MetricOperationDuration, new ExplicitBucketHistogramConfiguration
        {
            Boundaries = new double[] { 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10, 30 }
        })
        .AddOtlpExporter())
    .WithTracing(t => t
        .AddSource(MailgunTelemetry.ActivitySourceName)
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());
```

Histogram bucket boundaries are a collector concern. The OpenTelemetry SDK's default buckets are tuned for milliseconds, so configure a seconds-scale view (as above) or a Radiant `LatencyBuckets` preset for the two duration histograms.

### What the host should add

The library does not emit process or runtime metrics; those belong to the host (Radiant includes them, or add `OpenTelemetry.Instrumentation.Runtime`). On .NET 8 and later, `System.Net.Http` also emits `http.client.request.duration` and an HTTP client span beneath `stage:request`; subscribing to it adds DNS/connection detail but is not required to use the metrics below.

## What an operator can answer

| Question | Where to look |
| --- | --- |
| Is mail going out? | `sendwithmailgun_client_operations_total{mailgun_operation="send", mailgun_outcome="success"}` rate |
| Why did sends fail? | same counter grouped by `mailgun_outcome` and `error_type` (a status code such as `401`, or an exception type) |
| Is Mailgun slow, or are we? | `sendwithmailgun_client_stage_duration_seconds` by `mailgun_stage`: `request` is the network round trip to Mailgun, `deserialize` is local parsing |
| Are calls piling up? | `sendwithmailgun_client_active_operations` |
| Which region/endpoint is affected? | `server_address` / `server_port` labels |
| Are we sending to bad addresses? | `sendwithmailgun_validation_results_total` by `mailgun_validation_result` and `mailgun_validation_risk` |
| Which version is deployed? | `sendwithmailgun_build_info` |
| Why was this particular send slow or failed? | the `mailgun send` span in Tempo: status, `error.type`, `http.response.status_code`, exception event, and stage children |

## Metrics catalog

Instrument names are dotted; a Prometheus exporter rewrites them to the snake-case names shown (adding `_total`, `_seconds`, `_bytes`, and histogram suffixes). The constant names are fields on `MailgunTelemetry`.

| Instrument (constant) | Kind | Unit | Labels | Prometheus name | Description |
| --- | --- | --- | --- | --- | --- |
| `sendwithmailgun.client.operation.duration` (`MetricOperationDuration`) | Histogram | `s` | `mailgun.operation`, `mailgun.outcome`, `server.address`, `server.port`, `http.response.status_code`*, `error.type`* | `sendwithmailgun_client_operation_duration_seconds` | End-to-end duration of a send or validation, from the start of the HTTP request to the parsed result. |
| `sendwithmailgun.client.operations` (`MetricOperations`) | Counter | `{operation}` | same as above | `sendwithmailgun_client_operations_total` | Completed operations by outcome. |
| `sendwithmailgun.client.active_operations` (`MetricActiveOperations`) | UpDownCounter | `{operation}` | `mailgun.operation`, `server.address`, `server.port` | `sendwithmailgun_client_active_operations` | Operations currently in flight (concurrency against Mailgun). |
| `sendwithmailgun.client.stage.duration` (`MetricStageDuration`) | Histogram | `s` | `mailgun.operation`, `mailgun.stage`, `mailgun.stage.outcome` | `sendwithmailgun_client_stage_duration_seconds` | Duration of each stage. The `_count` series is the per-stage event counter. |
| `sendwithmailgun.send.recipients` (`MetricSendRecipients`) | Histogram | `{recipient}` | `mailgun.body.format` | `sendwithmailgun_send_recipients` | Recipients per send (to, cc, and bcc combined, comma-separated entries). |
| `sendwithmailgun.send.body.size` (`MetricSendBodySize`) | Histogram | `By` | `mailgun.body.format` | `sendwithmailgun_send_body_size_bytes` | UTF-8 size of the message body per send. |
| `sendwithmailgun.validation.results` (`MetricValidationResults`) | Counter | `{validation}` | `mailgun.validation.result`, `mailgun.validation.risk` | `sendwithmailgun_validation_results_total` | Successful validations by Mailgun verdict. |
| `sendwithmailgun.build.info` (`MetricBuildInfo`) | ObservableGauge | `1` | `sendwithmailgun.version` | `sendwithmailgun_build_info` | Always 1; labeled with the library version. Published once a `MailgunSender` or `MailgunValidator` has been constructed. |

\* `http.response.status_code` is present when Mailgun returned a response. `error.type` is present on every outcome other than `success`.

Argument validation failures (`ArgumentNullException` thrown before any request) are caller bugs, not Mailgun operations, and are not counted.

## Label values

| Label | Values |
| --- | --- |
| `mailgun.operation` | `send`, `validate` |
| `mailgun.outcome` | `success`: 200 with usable content. `http_error`: non-200 status (method returns `null`). `empty_response`: 200 with no body (returns `null`). `malformed_response`: 200 but no message ID / null result (returns `null`). `no_response`: the HTTP layer returned no response (returns `null`). `timeout`: cancelled without the caller's token being signaled (throws). `cancelled`: caller's token signaled (throws). `error`: any other exception, for example connection refused or invalid JSON (throws). |
| `error.type` | For `http_error`, the status code (`401`, `429`, `500`, ...). For exceptions, the exception's full type name (`System.Net.Http.HttpRequestException`, `System.Text.Json.JsonException`, `System.Threading.Tasks.TaskCanceledException`, ...). Otherwise the outcome value. |
| `mailgun.stage` | `request` (HTTP round trip to Mailgun), `deserialize` (parsing the response body). |
| `mailgun.stage.outcome` | `success`, `error` |
| `mailgun.body.format` | `text`, `html` |
| `mailgun.validation.result` | `Deliverable`, `Undeliverable`, `DoNotSend`, `CatchAll`, `Unknown` |
| `mailgun.validation.risk` | `High`, `Medium`, `Low`, `Unknown` |
| `server.address` / `server.port` | Host and port of the configured base URL (for example `api.mailgun.net` / `443`, `api.eu.mailgun.net` / `443`). |

## Spans catalog

| Span name | Kind | Parent | Attributes | Status |
| --- | --- | --- | --- | --- |
| `mailgun send` (`SpanSend`) | Client | `Activity.Current` of the caller (for example Watson's request span) | `mailgun.operation`, `mailgun.domain`, `mailgun.recipient.count`, `mailgun.body.format`, `server.address`, `server.port`, `http.request.method`, `url.full`, `http.response.status_code`, `mailgun.message.id` (on success), `mailgun.outcome`, `error.type` (on failure) | `Ok` on success, `Error` otherwise. Exceptions are attached as an `exception` event (`exception.type`, `exception.message`, `exception.stacktrace`). |
| `mailgun validate` (`SpanValidate`) | Client | caller | `mailgun.operation`, `server.address`, `server.port`, `http.request.method`, `url.full`, `http.response.status_code`, `mailgun.validation.result`, `mailgun.validation.risk` (on success), `mailgun.outcome`, `error.type` (on failure) | as above |
| `stage:request` | Internal | the operation span | `mailgun.operation`, `mailgun.stage`, `mailgun.stage.outcome`, `error.type` and `exception` event on failure | `Ok` / `Error` |
| `stage:deserialize` | Internal | the operation span | as above | `Ok` / `Error` |

A typical trace in Tempo:

```
POST /orders                       (your service, e.g. Watson server span)
  └─ mailgun send                  (SendWithMailgun client span)
       ├─ stage:request
       │    └─ POST                (System.Net.Http client span, if subscribed)
       └─ stage:deserialize
```

## Trace context propagation

The operation span starts from `Activity.Current`, so it joins whatever trace the caller is in, including background work as long as the caller flows `Activity.Current` (the default for `async`/`await` and `Task.Run`). The outbound HTTP request carries a W3C `traceparent` (and `tracestate` when set): on .NET and .NET Standard hosts `HttpClient` injects it automatically; on .NET Framework (`net462`, `net48`) the library injects it explicitly. Mailgun does not participate in tracing, so the header only matters for proxies or egress gateways that do.

## Privacy and cardinality

- Metric labels are bounded: the operation, outcome, stage, body format, validation verdict enums, HTTP status codes, exception type names, the configured host and port, and the library version.
- Identifiers (`mailgun.domain`, `mailgun.message.id`, `url.full`) appear only on spans, never on metrics.
- Email addresses, subjects, bodies, and API keys are never recorded on spans or metrics. The validated address is not recorded. Tests enforce this.
- Exception messages are attached to spans as the standard `exception` event. The library's existing `Exception.Data` entries (which do contain message fields for the caller's own diagnostics) are not exported.
- Instrumentation is best-effort: every telemetry call is guarded and can never change the result of a send or validation.

## Recommended PromQL alerts

```promql
# Send failure ratio above 5% for 10 minutes
(
  sum(rate(sendwithmailgun_client_operations_total{mailgun_operation="send", mailgun_outcome!="success"}[5m]))
  /
  sum(rate(sendwithmailgun_client_operations_total{mailgun_operation="send"}[5m]))
) > 0.05

# Authentication failures (bad or revoked API key): any 401/403 in 5 minutes
sum(increase(sendwithmailgun_client_operations_total{mailgun_outcome="http_error", error_type=~"401|403"}[5m])) > 0

# Rate limited by Mailgun
sum(increase(sendwithmailgun_client_operations_total{error_type="429"}[5m])) > 0

# Mailgun round trip p95 above 5 seconds
histogram_quantile(0.95,
  sum by (le) (rate(sendwithmailgun_client_stage_duration_seconds_bucket{mailgun_stage="request"}[5m]))
) > 5

# Requests piling up against Mailgun
sum(sendwithmailgun_client_active_operations) > 50

# No successful sends in 1 hour while the app is up (tune to your send volume)
sum(increase(sendwithmailgun_client_operations_total{mailgun_operation="send", mailgun_outcome="success"}[1h])) == 0
```

## Suggested dashboard

SendWithMailgun is a library, so it ships no compose stack or dashboards of its own. A host service should add a **Mailgun** panel row to its Integrations dashboard (per the host's Grafana provisioning):

| Panel | Query |
| --- | --- |
| Operations by outcome | `sum by (mailgun_operation, mailgun_outcome) (rate(sendwithmailgun_client_operations_total[5m]))` |
| Errors by type | `sum by (error_type) (rate(sendwithmailgun_client_operations_total{mailgun_outcome!="success"}[5m]))` |
| Operation p50/p95/p99 | `histogram_quantile(0.95, sum by (le, mailgun_operation) (rate(sendwithmailgun_client_operation_duration_seconds_bucket[5m])))` |
| Stage p95 | `histogram_quantile(0.95, sum by (le, mailgun_stage) (rate(sendwithmailgun_client_stage_duration_seconds_bucket[5m])))` |
| In flight | `sum by (mailgun_operation) (sendwithmailgun_client_active_operations)` |
| Recipients per send (avg) | `sum(rate(sendwithmailgun_send_recipients_sum[5m])) / sum(rate(sendwithmailgun_send_recipients_count[5m]))` |
| Validation verdicts | `sum by (mailgun_validation_result, mailgun_validation_risk) (rate(sendwithmailgun_validation_results_total[5m]))` |
| Library version | `sendwithmailgun_build_info` |

Link the panels to Tempo with a `{ name = "mailgun send" && status = error }` TraceQL query to jump from a failure spike to a representative trace.

## Testing

`src/Test.Shared/TelemetrySuites.cs` proves the contract with an in-memory `MeterListener` and `ActivityListener` (`TelemetryCapture`). It covers every instrument being published, units, build info, success spans and stage hierarchy, every outcome (`http_error`, `empty_response`, `malformed_response`, invalid JSON, connection refused, cancellation), `traceparent` propagation, the absence of sensitive data, and the no-listener path. The suites run under all three runners (`Test.Automated`, `Test.Xunit`, `Test.Nunit`).
