![alt tag](https://raw.githubusercontent.com/jchristn/SendWithMailgun/main/SendWithMailgun/assets/icon.ico)

# SendWithMailgun

[![NuGet Version](https://img.shields.io/nuget/v/SendWithMailgun.svg?style=flat)](https://www.nuget.org/packages/SendWithMailgun/) [![NuGet](https://img.shields.io/nuget/dt/SendWithMailgun.svg)](https://www.nuget.org/packages/SendWithMailgun) 

SendWithMailgun is a really small class library with only one goal in mind: sending an email with Mailgun.  That's it.  Need other features?  Not here, sorry :(

## New in v1.0.0

- Initial release.

## Help or feedback

First things first - do you need help or have feedback?  File an issue!  Happy to help.

## Simple Example
```csharp
using SendWithMailgun;

MailgunSender sender = new MailgunSender("[mydomain.com", "[apikey]");
sender.Logger = Console.WriteLine; // if you want log messages

string id = null;
id = sender.Send("to", "from", "subject", "body");       // text
id = sender.Send("to", "from", "subject", "html", true); // html
```