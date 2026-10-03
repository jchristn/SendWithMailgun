# Change Log

## Current Version

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
