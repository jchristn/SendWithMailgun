# Change Log

## Current Version

v1.2.0

- Add built-in telemetry.  The library emits metrics through a `Meter` and spans through an `ActivitySource`, both named `SendWithMailgun`, with no exporter dependency and near-zero cost when nothing is listening.  See `TELEMETRY.md`
- Metrics: operation count and duration by operation/outcome/status/error type, in-flight operations, per-stage duration (`request`, `deserialize`), recipients and body size per send, validation verdicts by result/risk, and a build-info gauge
- Traces: a `mailgun send` / `mailgun validate` client span per call with `stage:request` and `stage:deserialize` children, explicit status, and exception events.  W3C `traceparent` flows to Mailgun (HttpClient on .NET; injected explicitly on .NET Framework)
- Non-200, empty, and malformed responses (which still return `null`) are now distinguishable by outcome in telemetry
- Sending no longer throws `NullReferenceException` when Mailgun returns 200 with a JSON `null` body or a null `id`; it returns `null` and records `malformed_response`
- Add `System.Diagnostics.DiagnosticSource` 10.0.12 dependency for `netstandard2.1`, `net462`, and `net48` (in-box on `net8.0` and `net10.0`)

v1.1.8

- Fix: `MailgunValidator` threw `JsonException` when Mailgun returned `result` values `do_not_send` or `catch_all`, because System.Text.Json ignores `[EnumMember]`.  `ResultEnum` and `RiskEnum` now use a converter that honors the Mailgun wire values
- Unrecognized or null `result` and `risk` values now map to `Unknown` instead of throwing
- `MailgunValidationResult` now serializes `Result` and `Risk` using Mailgun wire values (for example `do_not_send` rather than `DoNotSend`); both forms are accepted when reading
- Add Touchstone-based test infrastructure (`Test.Shared`, `Test.Automated`, `Test.Xunit`, `Test.Nunit`) with positive and negative coverage against an in-process mock Mailgun server

v1.1.x

- Dependency updates
- Remove Newtonsoft.Json
- Add support for email validation

## Previous Versions

v1.0.0

- Initial release
