![alt tag](https://raw.githubusercontent.com/jchristn/SendWithMailgun/main/src/SendWithMailgun/assets/icon.ico)

# SendWithMailgun

[![NuGet Version](https://img.shields.io/nuget/v/SendWithMailgun.svg?style=flat)](https://www.nuget.org/packages/SendWithMailgun/) [![NuGet](https://img.shields.io/nuget/dt/SendWithMailgun.svg)](https://www.nuget.org/packages/SendWithMailgun) 

SendWithMailgun is a really small class library with only one goal in mind: sending an email with Mailgun.  That's it.  Need other features?  Not here, sorry :(

## New in v1.1.x

- Dependency updates
- Remove Newtonsoft.Json
- Add support for email validation
- v1.1.8: validation results of `do_not_send` and `catch_all` now deserialize correctly; unrecognized `result` or `risk` values map to `Unknown` instead of throwing

## Help or feedback

First things first - do you need help or have feedback?  File an issue!  Happy to help.

## Simple Email Example
```csharp
using SendWithMailgun;

MailgunSender sender = new MailgunSender("[mydomain.com]", "[apikey]");
sender.Logger = Console.WriteLine; // if you want log messages

string id = null;
id = sender.Send("to", "from", "subject", "body");       // text
id = sender.Send("to", "from", "subject", "html", true); // html
Console.WriteLine("Message ID: " + id);
```

## Simple Validation Example
```csharp
using SendWithMailgun;

MailgunValidator validator = new MailgunValidator("[apikey]");
MailgunValidationResult result = validator.Validate("[emailaddress]");
Console.WriteLine("Result: " + result.Result.ToString() + " risk level: " + result.Risk.ToString());
```

`Validate` and `ValidateAsync` return `null` when Mailgun responds with a non-200 status code or an empty body.  `Result` and `Risk` map Mailgun's wire values (`deliverable`, `undeliverable`, `do_not_send`, `catch_all`, `unknown` and `high`, `medium`, `low`, `unknown`) to `ResultEnum` and `RiskEnum`.  Any value not recognized by this version of the library is mapped to `Unknown`.

## Regional Endpoints

Both classes accept a base URL, for example to use Mailgun's EU region:

```csharp
MailgunSender sender = new MailgunSender("[mydomain.com]", "[apikey]", "https://api.eu.mailgun.net/v3/");
MailgunValidator validator = new MailgunValidator("[apikey]", "https://api.eu.mailgun.net/v4/");
```

## Running the Tests

Tests are written once in `src/Test.Shared` using [Touchstone](https://github.com/jchristn/touchstone) and run through three runners.  They use an in-process mock Mailgun server bound to `127.0.0.1`, so no Mailgun account or API key is required.

```bash
dotnet run --project src/Test.Automated -f net10.0                      # console runner
dotnet run --project src/Test.Automated -f net10.0 -- --results out.json # console runner with JSON results
dotnet test src/Test.Xunit                                              # xUnit
dotnet test src/Test.Nunit                                              # NUnit
```

`src/Test` is an interactive console application for sending and validating against the real Mailgun API with your own domain and keys.
