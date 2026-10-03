namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using SendWithMailgun;
    using Touchstone.Core;

    /// <summary>
    /// Shared suites covering <see cref="MailgunSender"/>.
    /// </summary>
    public static class SenderSuites
    {
        #region Public-Members

        /// <summary>
        /// Domain used by tests.
        /// </summary>
        public const string Domain = "mg.example.com";

        /// <summary>
        /// API key used by tests.
        /// </summary>
        public const string ApiKey = "key-0123456789abcdef";

        /// <summary>
        /// Successful send response body.
        /// </summary>
        public const string SuccessBody = "{\"id\":\"<20260101000000.1.ABCDEF@mg.example.com>\",\"message\":\"Queued. Thank you.\"}";

        /// <summary>
        /// Message ID contained in <see cref="SuccessBody"/>.
        /// </summary>
        public const string SuccessId = "<20260101000000.1.ABCDEF@mg.example.com>";

        #endregion

        #region Public-Methods

        /// <summary>
        /// Constructor and property behavior.
        /// </summary>
        /// <returns>Suite.</returns>
        public static TestSuiteDescriptor ConstructorSuite()
        {
            const string s = "SenderConstructor";

            return new TestSuiteDescriptor(
                suiteId: s,
                displayName: "MailgunSender construction",
                cases: new List<TestCaseDescriptor>
                {
                    SuiteHelpers.Case(s, "DefaultBaseUrl", "Sender: valid arguments use default v3 base URL", () =>
                    {
                        MailgunSender sender = new MailgunSender(Domain, ApiKey);
                        TestAssert.Equal(Domain, sender.Domain, "Domain");
                        TestAssert.Equal(ApiKey, sender.ApiKey, "ApiKey");
                        TestAssert.Equal("https://api.mailgun.net/v3/", sender.BaseUrl, "BaseUrl");
                        TestAssert.Null(sender.Logger, "Logger");
                    }),

                    SuiteHelpers.Case(s, "CustomBaseUrlAppendsSlash", "Sender: custom base URL without trailing slash gets one appended", () =>
                    {
                        MailgunSender sender = new MailgunSender(Domain, ApiKey, "https://api.eu.mailgun.net/v3");
                        TestAssert.Equal("https://api.eu.mailgun.net/v3/", sender.BaseUrl, "BaseUrl");
                    }),

                    SuiteHelpers.Case(s, "CustomBaseUrlKeepsSlash", "Sender: custom base URL with trailing slash is unchanged", () =>
                    {
                        MailgunSender sender = new MailgunSender(Domain, ApiKey, "https://api.eu.mailgun.net/v3/");
                        TestAssert.Equal("https://api.eu.mailgun.net/v3/", sender.BaseUrl, "BaseUrl");
                    }),

                    SuiteHelpers.Case(s, "LoggerSettable", "Sender: Logger can be assigned and cleared", () =>
                    {
                        MailgunSender sender = new MailgunSender(Domain, ApiKey);
                        Action<string> logger = msg => { };
                        sender.Logger = logger;
                        TestAssert.True(ReferenceEquals(logger, sender.Logger), "Logger was not retained");
                        sender.Logger = null;
                        TestAssert.Null(sender.Logger, "Logger");
                    }),

                    SuiteHelpers.Case(s, "NullDomainThrows", "Sender: null domain throws ArgumentNullException", () =>
                        TestAssert.ThrowsArgumentNull(() => new MailgunSender(null, ApiKey), "domain")),

                    SuiteHelpers.Case(s, "EmptyDomainThrows", "Sender: empty domain throws ArgumentNullException", () =>
                        TestAssert.ThrowsArgumentNull(() => new MailgunSender("", ApiKey), "domain")),

                    SuiteHelpers.Case(s, "NullApiKeyThrows", "Sender: null API key throws ArgumentNullException", () =>
                        TestAssert.ThrowsArgumentNull(() => new MailgunSender(Domain, null), "apiKey")),

                    SuiteHelpers.Case(s, "EmptyApiKeyThrows", "Sender: empty API key throws ArgumentNullException", () =>
                        TestAssert.ThrowsArgumentNull(() => new MailgunSender(Domain, ""), "apiKey")),

                    SuiteHelpers.Case(s, "NullBaseUrlThrows", "Sender: null base URL throws ArgumentNullException", () =>
                        TestAssert.ThrowsArgumentNull(() => new MailgunSender(Domain, ApiKey, null), "baseUrl")),

                    SuiteHelpers.Case(s, "EmptyBaseUrlThrows", "Sender: empty base URL throws ArgumentNullException", () =>
                        TestAssert.ThrowsArgumentNull(() => new MailgunSender(Domain, ApiKey, ""), "baseUrl"))
                });
        }

        /// <summary>
        /// Argument validation for send methods.
        /// </summary>
        /// <returns>Suite.</returns>
        public static TestSuiteDescriptor ArgumentValidationSuite()
        {
            const string s = "SenderArguments";

            return new TestSuiteDescriptor(
                suiteId: s,
                displayName: "MailgunSender argument validation",
                cases: new List<TestCaseDescriptor>
                {
                    SuiteHelpers.Case(s, "NullToThrows", "SendAsync: null 'to' throws ArgumentNullException", async ct =>
                        await TestAssert.ThrowsArgumentNullAsync(() => Unused().SendAsync(null, "a@b.com", "s", "b", token: ct), "to").ConfigureAwait(false)),

                    SuiteHelpers.Case(s, "EmptyToThrows", "SendAsync: empty 'to' throws ArgumentNullException", async ct =>
                        await TestAssert.ThrowsArgumentNullAsync(() => Unused().SendAsync("", "a@b.com", "s", "b", token: ct), "to").ConfigureAwait(false)),

                    SuiteHelpers.Case(s, "NullFromThrows", "SendAsync: null 'from' throws ArgumentNullException", async ct =>
                        await TestAssert.ThrowsArgumentNullAsync(() => Unused().SendAsync("a@b.com", null, "s", "b", token: ct), "from").ConfigureAwait(false)),

                    SuiteHelpers.Case(s, "EmptyFromThrows", "SendAsync: empty 'from' throws ArgumentNullException", async ct =>
                        await TestAssert.ThrowsArgumentNullAsync(() => Unused().SendAsync("a@b.com", "", "s", "b", token: ct), "from").ConfigureAwait(false)),

                    SuiteHelpers.Case(s, "NullBodyThrows", "SendAsync: null body throws ArgumentNullException", async ct =>
                        await TestAssert.ThrowsArgumentNullAsync(() => Unused().SendAsync("a@b.com", "c@d.com", "s", null, token: ct), "body").ConfigureAwait(false)),

                    SuiteHelpers.Case(s, "EmptyBodyThrows", "SendAsync: empty body throws ArgumentNullException", async ct =>
                        await TestAssert.ThrowsArgumentNullAsync(() => Unused().SendAsync("a@b.com", "c@d.com", "s", "", token: ct), "body").ConfigureAwait(false)),

                    SuiteHelpers.Case(s, "SyncNullToThrows", "Send: null 'to' surfaces ArgumentNullException (wrapped in AggregateException)", () =>
                    {
                        AggregateException e = TestAssert.Throws<AggregateException>(() => Unused().Send(null, "a@b.com", "s", "b"));
                        ArgumentNullException? inner = e.InnerException as ArgumentNullException;
                        TestAssert.NotNull(inner, "InnerException as ArgumentNullException");
                        TestAssert.Equal("to", inner!.ParamName, "ParamName");
                    }),

                    SuiteHelpers.Case(s, "ValidationPrecedesNetwork", "SendAsync: invalid arguments fail before any HTTP request is made", async ct =>
                    {
                        using (MockMailgunServer server = new MockMailgunServer())
                        {
                            MailgunSender sender = new MailgunSender(Domain, ApiKey, server.RootUrl + "v3/");
                            await TestAssert.ThrowsAsync<ArgumentNullException>(() => sender.SendAsync("a@b.com", "c@d.com", "s", null, token: ct)).ConfigureAwait(false);
                            TestAssert.Equal(0, server.Requests.Count, "Request count");
                        }
                    })
                });
        }

        /// <summary>
        /// Shape of the outbound HTTP request.
        /// </summary>
        /// <returns>Suite.</returns>
        public static TestSuiteDescriptor RequestSuite()
        {
            const string s = "SenderRequest";

            return new TestSuiteDescriptor(
                suiteId: s,
                displayName: "MailgunSender outbound request",
                cases: new List<TestCaseDescriptor>
                {
                    SuiteHelpers.Case(s, "PostsToMessagesEndpoint", "SendAsync: POSTs to {baseUrl}{domain}/messages", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            await Sender(server).SendAsync("to@example.com", "from@example.com", "Subject", "Body", token: ct).ConfigureAwait(false);
                            RecordedRequest req = server.SingleRequest();
                            TestAssert.Equal("POST", req.Method, "Method");
                            TestAssert.Equal("/v3/" + Domain + "/messages", req.Path, "Path");
                        }
                    }),

                    SuiteHelpers.Case(s, "BaseUrlWithoutSlash", "SendAsync: base URL without trailing slash still builds the correct path", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            MailgunSender sender = new MailgunSender(Domain, ApiKey, server.RootUrl + "custom/v3");
                            await sender.SendAsync("to@example.com", "from@example.com", "Subject", "Body", token: ct).ConfigureAwait(false);
                            TestAssert.Equal("/custom/v3/" + Domain + "/messages", server.SingleRequest().Path, "Path");
                        }
                    }),

                    SuiteHelpers.Case(s, "BasicAuth", "SendAsync: uses HTTP basic auth with user 'api' and the API key", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            await Sender(server).SendAsync("to@example.com", "from@example.com", "Subject", "Body", token: ct).ConfigureAwait(false);
                            TestAssert.Equal("api:" + ApiKey, server.SingleRequest().DecodeBasicAuthorization(), "Decoded authorization");
                        }
                    }),

                    SuiteHelpers.Case(s, "FormEncoded", "SendAsync: body is application/x-www-form-urlencoded", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            await Sender(server).SendAsync("to@example.com", "from@example.com", "Subject", "Body", token: ct).ConfigureAwait(false);
                            string? contentType = server.SingleRequest().ContentType;
                            TestAssert.True(contentType != null && contentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase),
                                "Unexpected content type '" + contentType + "'");
                        }
                    }),

                    SuiteHelpers.Case(s, "PlainTextFields", "SendAsync: plain text send includes domain/to/from/subject/text and no html", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            await Sender(server).SendAsync("to@example.com", "from@example.com", "Hello", "Plain body", token: ct).ConfigureAwait(false);
                            Dictionary<string, string> form = server.SingleRequest().Form;
                            TestAssert.Equal(Domain, Field(form, "domain"), "domain");
                            TestAssert.Equal("to@example.com", Field(form, "to"), "to");
                            TestAssert.Equal("from@example.com", Field(form, "from"), "from");
                            TestAssert.Equal("Hello", Field(form, "subject"), "subject");
                            TestAssert.Equal("Plain body", Field(form, "text"), "text");
                            TestAssert.False(form.ContainsKey("html"), "html field should not be present");
                            TestAssert.False(form.ContainsKey("cc"), "cc field should not be present");
                            TestAssert.False(form.ContainsKey("bcc"), "bcc field should not be present");
                        }
                    }),

                    SuiteHelpers.Case(s, "HtmlFields", "SendAsync: isHtml=true sends body as 'html' and omits 'text'", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            await Sender(server).SendAsync("to@example.com", "from@example.com", "Hello", "<b>Hi</b>", isHtml: true, token: ct).ConfigureAwait(false);
                            Dictionary<string, string> form = server.SingleRequest().Form;
                            TestAssert.Equal("<b>Hi</b>", Field(form, "html"), "html");
                            TestAssert.False(form.ContainsKey("text"), "text field should not be present");
                        }
                    }),

                    SuiteHelpers.Case(s, "CcBccIncluded", "SendAsync: cc and bcc are sent when provided", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            await Sender(server).SendAsync("to@example.com", "from@example.com", "Hello", "Body", false, "cc@example.com", "bcc@example.com", ct).ConfigureAwait(false);
                            Dictionary<string, string> form = server.SingleRequest().Form;
                            TestAssert.Equal("cc@example.com", Field(form, "cc"), "cc");
                            TestAssert.Equal("bcc@example.com", Field(form, "bcc"), "bcc");
                        }
                    }),

                    SuiteHelpers.Case(s, "EmptyCcBccOmitted", "SendAsync: empty cc and bcc are omitted", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            await Sender(server).SendAsync("to@example.com", "from@example.com", "Hello", "Body", false, "", "", ct).ConfigureAwait(false);
                            Dictionary<string, string> form = server.SingleRequest().Form;
                            TestAssert.False(form.ContainsKey("cc"), "cc field should not be present");
                            TestAssert.False(form.ContainsKey("bcc"), "bcc field should not be present");
                        }
                    }),

                    SuiteHelpers.Case(s, "NullSubjectOmitted", "SendAsync: null subject is allowed and omitted", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            string? id = await Sender(server).SendAsync("to@example.com", "from@example.com", null, "Body", token: ct).ConfigureAwait(false);
                            TestAssert.Equal(SuccessId, id, "Returned ID");
                            TestAssert.False(server.SingleRequest().Form.ContainsKey("subject"), "subject field should not be present");
                        }
                    }),

                    SuiteHelpers.Case(s, "EmptySubjectOmitted", "SendAsync: empty subject is allowed and omitted", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            await Sender(server).SendAsync("to@example.com", "from@example.com", "", "Body", token: ct).ConfigureAwait(false);
                            TestAssert.False(server.SingleRequest().Form.ContainsKey("subject"), "subject field should not be present");
                        }
                    }),

                    SuiteHelpers.Case(s, "MultipleRecipients", "SendAsync: comma-separated recipients are passed through unchanged", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            string to = "a@example.com, b@example.com,c@example.com";
                            string cc = "d@example.com,e@example.com";
                            string bcc = "f@example.com,g@example.com";
                            await Sender(server).SendAsync(to, "from@example.com", "Hello", "Body", false, cc, bcc, ct).ConfigureAwait(false);
                            Dictionary<string, string> form = server.SingleRequest().Form;
                            TestAssert.Equal(to, Field(form, "to"), "to");
                            TestAssert.Equal(cc, Field(form, "cc"), "cc");
                            TestAssert.Equal(bcc, Field(form, "bcc"), "bcc");
                        }
                    }),

                    SuiteHelpers.Case(s, "DisplayNameAddress", "SendAsync: display-name style from address is preserved", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            string from = "Example Sender <sender@example.com>";
                            await Sender(server).SendAsync("to@example.com", from, "Hello", "Body", token: ct).ConfigureAwait(false);
                            TestAssert.Equal(from, Field(server.SingleRequest().Form, "from"), "from");
                        }
                    }),

                    SuiteHelpers.Case(s, "SpecialCharactersRoundTrip", "SendAsync: reserved, unicode and multi-line content is encoded and round-trips", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            string subject = "Q&A = 100% + more? éè 你好 😀";
                            string body = "Line 1\r\nLine 2 & <tag attr=\"x\">\n#hash ?query=1&x=2 +plus";
                            await Sender(server).SendAsync("to@example.com", "from@example.com", subject, body, token: ct).ConfigureAwait(false);
                            Dictionary<string, string> form = server.SingleRequest().Form;
                            TestAssert.Equal(subject, Field(form, "subject"), "subject");
                            TestAssert.Equal(body, Field(form, "text"), "text");
                        }
                    }),

                    SuiteHelpers.Case(s, "ApiKeySpecialCharacters", "SendAsync: API key containing reserved characters is sent intact", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            string key = "k:e/y+=&?%";
                            MailgunSender sender = new MailgunSender(Domain, key, server.RootUrl + "v3/");
                            await sender.SendAsync("to@example.com", "from@example.com", "s", "b", token: ct).ConfigureAwait(false);
                            TestAssert.Equal("api:" + key, server.SingleRequest().DecodeBasicAuthorization(), "Decoded authorization");
                        }
                    }),

                    SuiteHelpers.Case(s, "LargeBody", "SendAsync: large body (256 KB) is transmitted in full", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            string body = new string('x', 256 * 1024);
                            await Sender(server).SendAsync("to@example.com", "from@example.com", "s", body, token: ct).ConfigureAwait(false);
                            TestAssert.Equal(body.Length, Field(server.SingleRequest().Form, "text").Length, "text length");
                        }
                    }),

                    SuiteHelpers.Case(s, "SequentialSends", "SendAsync: the same sender instance can send repeatedly", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            MailgunSender sender = Sender(server);
                            for (int i = 0; i < 3; i++)
                            {
                                string? id = await sender.SendAsync("to" + i + "@example.com", "from@example.com", "s", "b", token: ct).ConfigureAwait(false);
                                TestAssert.Equal(SuccessId, id, "Returned ID #" + i);
                            }

                            List<RecordedRequest> requests = server.Requests;
                            TestAssert.Equal(3, requests.Count, "Request count");
                            TestAssert.Equal("to2@example.com", Field(requests[2].Form, "to"), "Third request 'to'");
                        }
                    })
                });
        }

        /// <summary>
        /// Handling of HTTP responses.
        /// </summary>
        /// <returns>Suite.</returns>
        public static TestSuiteDescriptor ResponseSuite()
        {
            const string s = "SenderResponse";

            return new TestSuiteDescriptor(
                suiteId: s,
                displayName: "MailgunSender response handling",
                cases: new List<TestCaseDescriptor>
                {
                    SuiteHelpers.Case(s, "ReturnsId", "SendAsync: 200 with id returns the message ID", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            string? id = await Sender(server).SendAsync("to@example.com", "from@example.com", "s", "b", token: ct).ConfigureAwait(false);
                            TestAssert.Equal(SuccessId, id, "Returned ID");
                        }
                    }),

                    SuiteHelpers.Case(s, "SyncReturnsId", "Send: synchronous send returns the message ID", () =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            string id = Sender(server).Send("to@example.com", "from@example.com", "s", "b");
                            TestAssert.Equal(SuccessId, id, "Returned ID");
                            TestAssert.Equal(1, server.Requests.Count, "Request count");
                        }
                    }),

                    SuiteHelpers.Case(s, "MissingIdReturnsNull", "SendAsync: 200 without an id returns null", async ct =>
                    {
                        using (MockMailgunServer server = Respond(200, "{\"message\":\"Queued. Thank you.\"}"))
                        {
                            string? id = await Sender(server).SendAsync("to@example.com", "from@example.com", "s", "b", token: ct).ConfigureAwait(false);
                            TestAssert.Null(id, "Returned ID");
                        }
                    }),

                    SuiteHelpers.Case(s, "EmptyBodyReturnsNull", "SendAsync: 200 with an empty body returns null", async ct =>
                    {
                        using (MockMailgunServer server = Respond(200, ""))
                        {
                            string? id = await Sender(server).SendAsync("to@example.com", "from@example.com", "s", "b", token: ct).ConfigureAwait(false);
                            TestAssert.Null(id, "Returned ID");
                        }
                    }),

                    SuiteHelpers.Case(s, "ExtraFieldsIgnored", "SendAsync: unknown response fields are ignored", async ct =>
                    {
                        using (MockMailgunServer server = Respond(200, "{\"id\":\"abc\",\"message\":\"ok\",\"extra\":{\"nested\":[1,2,3]}}"))
                        {
                            string? id = await Sender(server).SendAsync("to@example.com", "from@example.com", "s", "b", token: ct).ConfigureAwait(false);
                            TestAssert.Equal("abc", id, "Returned ID");
                        }
                    }),

                    SuiteHelpers.Case(s, "Unauthorized401ReturnsNull", "SendAsync: 401 Unauthorized returns null", async ct =>
                        await AssertSendReturnsNull(401, "Forbidden", ct).ConfigureAwait(false)),

                    SuiteHelpers.Case(s, "BadRequest400ReturnsNull", "SendAsync: 400 Bad Request returns null", async ct =>
                        await AssertSendReturnsNull(400, "{\"message\":\"'from' parameter is not a valid address. please check documentation\"}", ct).ConfigureAwait(false)),

                    SuiteHelpers.Case(s, "NotFound404ReturnsNull", "SendAsync: 404 Not Found (unknown domain) returns null", async ct =>
                        await AssertSendReturnsNull(404, "{\"message\":\"Domain not found\"}", ct).ConfigureAwait(false)),

                    SuiteHelpers.Case(s, "TooManyRequests429ReturnsNull", "SendAsync: 429 Too Many Requests returns null", async ct =>
                        await AssertSendReturnsNull(429, "{\"message\":\"Too many requests\"}", ct).ConfigureAwait(false)),

                    SuiteHelpers.Case(s, "ServerError500ReturnsNull", "SendAsync: 500 Internal Server Error returns null", async ct =>
                        await AssertSendReturnsNull(500, "{\"message\":\"Internal error\"}", ct).ConfigureAwait(false)),

                    SuiteHelpers.Case(s, "NonOk2xxReturnsNull", "SendAsync: non-200 success status (202) returns null", async ct =>
                        await AssertSendReturnsNull(202, SuccessBody, ct).ConfigureAwait(false)),

                    SuiteHelpers.Case(s, "LoggerReceivesMessages", "SendAsync: Logger receives URL, status and message ID", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            List<string> logs = new List<string>();
                            MailgunSender sender = Sender(server);
                            sender.Logger = msg => { lock (logs) { logs.Add(msg); } };
                            await sender.SendAsync("to@example.com", "from@example.com", "s", "b", token: ct).ConfigureAwait(false);

                            string all = String.Join("\n", logs);
                            TestAssert.True(logs.TrueForAll(m => m.StartsWith("[MailgunSender] ", StringComparison.Ordinal)), "All log lines should carry the [MailgunSender] header");
                            TestAssert.True(all.Contains(server.RootUrl + "v3/" + Domain + "/messages"), "Log should contain the request URL");
                            TestAssert.True(all.Contains("200"), "Log should contain the status code");
                            TestAssert.True(all.Contains(SuccessId), "Log should contain the message ID");
                        }
                    }),

                    SuiteHelpers.Case(s, "NoLoggerIsSafe", "SendAsync: sending with no Logger assigned does not throw", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            MailgunSender sender = Sender(server);
                            sender.Logger = null;
                            string? id = await sender.SendAsync("to@example.com", "from@example.com", "s", "b", token: ct).ConfigureAwait(false);
                            TestAssert.Equal(SuccessId, id, "Returned ID");
                        }
                    })
                });
        }

        /// <summary>
        /// Exceptional conditions: malformed responses, network failure, cancellation.
        /// </summary>
        /// <returns>Suite.</returns>
        public static TestSuiteDescriptor FailureSuite()
        {
            const string s = "SenderFailure";

            return new TestSuiteDescriptor(
                suiteId: s,
                displayName: "MailgunSender failure handling",
                cases: new List<TestCaseDescriptor>
                {
                    SuiteHelpers.Case(s, "InvalidJsonThrows", "SendAsync: 200 with invalid JSON throws JsonException with context data", async ct =>
                    {
                        using (MockMailgunServer server = Respond(200, "this is not json"))
                        {
                            JsonException e = await TestAssert.ThrowsAsync<JsonException>(() =>
                                Sender(server).SendAsync("to@example.com", "from@example.com", "Subj", "Body", false, "cc@example.com", null, ct)).ConfigureAwait(false);

                            TestAssert.Equal(server.RootUrl + "v3/" + Domain + "/messages", e.Data["Url"] as string, "Data[Url]");
                            TestAssert.Equal("to@example.com", e.Data["To"] as string, "Data[To]");
                            TestAssert.Equal("from@example.com", e.Data["From"] as string, "Data[From]");
                            TestAssert.Equal("cc@example.com", e.Data["Cc"] as string, "Data[Cc]");
                            TestAssert.Equal("Subj", e.Data["Subject"] as string, "Data[Subject]");
                            TestAssert.Equal("Body", e.Data["Body"] as string, "Data[Body]");
                            TestAssert.Equal((object)false, e.Data["IsHtml"], "Data[IsHtml]");
                            TestAssert.Equal((object)200, e.Data["StatusCode"], "Data[StatusCode]");
                            TestAssert.Equal("this is not json", e.Data["Response"] as string, "Data[Response]");
                        }
                    }),

                    SuiteHelpers.Case(s, "JsonArrayThrows", "SendAsync: 200 with a JSON array (not an object) throws JsonException", async ct =>
                    {
                        using (MockMailgunServer server = Respond(200, "[1,2,3]"))
                        {
                            await TestAssert.ThrowsAsync<JsonException>(() =>
                                Sender(server).SendAsync("to@example.com", "from@example.com", "s", "b", token: ct)).ConfigureAwait(false);
                        }
                    }),

                    SuiteHelpers.Case(s, "ConnectionRefusedThrows", "SendAsync: unreachable server throws HttpRequestException with context data", async ct =>
                    {
                        string url = "http://127.0.0.1:" + MockMailgunServer.GetFreePort() + "/v3/";
                        MailgunSender sender = new MailgunSender(Domain, ApiKey, url);
                        HttpRequestException e = await TestAssert.ThrowsAsync<HttpRequestException>(() =>
                            sender.SendAsync("to@example.com", "from@example.com", "s", "b", token: ct)).ConfigureAwait(false);

                        TestAssert.Equal(url + Domain + "/messages", e.Data["Url"] as string, "Data[Url]");
                        TestAssert.Equal("to@example.com", e.Data["To"] as string, "Data[To]");
                        TestAssert.False(e.Data.Contains("StatusCode"), "Data should not contain StatusCode when no response was received");
                    }),

                    SuiteHelpers.Case(s, "PreCancelledThrows", "SendAsync: already-cancelled token throws OperationCanceledException without sending", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        using (CancellationTokenSource cts = new CancellationTokenSource())
                        {
                            cts.Cancel();
                            await TestAssert.ThrowsAsync<OperationCanceledException>(() =>
                                Sender(server).SendAsync("to@example.com", "from@example.com", "s", "b", token: cts.Token)).ConfigureAwait(false);
                            TestAssert.Equal(0, server.Requests.Count, "Request count");
                        }
                    }),

                    SuiteHelpers.Case(s, "CancelledInFlightThrows", "SendAsync: cancellation while awaiting the response throws OperationCanceledException", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        using (CancellationTokenSource cts = new CancellationTokenSource())
                        {
                            server.ResponseDelayMs = 5000;
                            cts.CancelAfter(250);
                            await TestAssert.ThrowsAsync<OperationCanceledException>(() =>
                                Sender(server).SendAsync("to@example.com", "from@example.com", "s", "b", token: cts.Token)).ConfigureAwait(false);
                        }
                    })
                });
        }

        #endregion

        #region Private-Methods

        private static MailgunSender Unused()
        {
            return new MailgunSender(Domain, ApiKey, "http://127.0.0.1:1/v3/");
        }

        private static MailgunSender Sender(MockMailgunServer server)
        {
            return new MailgunSender(Domain, ApiKey, server.RootUrl + "v3/");
        }

        private static MockMailgunServer Success()
        {
            return Respond(200, SuccessBody);
        }

        private static MockMailgunServer Respond(int statusCode, string body)
        {
            MockMailgunServer server = new MockMailgunServer();
            server.StatusCode = statusCode;
            server.ResponseBody = body;
            return server;
        }

        private static string Field(Dictionary<string, string> form, string key)
        {
            if (!form.TryGetValue(key, out string? val))
                throw new TestAssertionException("Form field '" + key + "' was not sent");
            return val;
        }

        private static async Task AssertSendReturnsNull(int statusCode, string body, CancellationToken ct)
        {
            using (MockMailgunServer server = Respond(statusCode, body))
            {
                string? id = await Sender(server).SendAsync("to@example.com", "from@example.com", "s", "b", token: ct).ConfigureAwait(false);
                TestAssert.Null(id, "Returned ID for HTTP " + statusCode);
                TestAssert.Equal(1, server.Requests.Count, "Request count");
            }
        }

        #endregion
    }
}
