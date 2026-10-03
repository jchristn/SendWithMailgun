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
    /// Shared suites covering <see cref="MailgunValidator"/>.
    /// </summary>
    public static class ValidatorSuites
    {
        #region Public-Members

        /// <summary>
        /// API key used by tests.
        /// </summary>
        public const string ApiKey = "pubkey-0123456789abcdef";

        /// <summary>
        /// Representative deliverable response body.
        /// </summary>
        public const string DeliverableBody =
            "{\"address\":\"user@example.com\",\"is_disposable_address\":false,\"is_role_address\":false,"
            + "\"reason\":[],\"result\":\"deliverable\",\"risk\":\"low\"}";

        #endregion

        #region Public-Methods

        /// <summary>
        /// Constructor and property behavior.
        /// </summary>
        /// <returns>Suite.</returns>
        public static TestSuiteDescriptor ConstructorSuite()
        {
            const string s = "ValidatorConstructor";

            return new TestSuiteDescriptor(
                suiteId: s,
                displayName: "MailgunValidator construction",
                cases: new List<TestCaseDescriptor>
                {
                    SuiteHelpers.Case(s, "DefaultBaseUrl", "Validator: valid API key uses default v4 base URL", () =>
                    {
                        MailgunValidator validator = new MailgunValidator(ApiKey);
                        TestAssert.Equal(ApiKey, validator.ApiKey, "ApiKey");
                        TestAssert.Equal("https://api.mailgun.net/v4/", validator.BaseUrl, "BaseUrl");
                        TestAssert.Null(validator.Logger, "Logger");
                    }),

                    SuiteHelpers.Case(s, "CustomBaseUrlAppendsSlash", "Validator: custom base URL without trailing slash gets one appended", () =>
                    {
                        MailgunValidator validator = new MailgunValidator(ApiKey, "https://api.eu.mailgun.net/v4");
                        TestAssert.Equal("https://api.eu.mailgun.net/v4/", validator.BaseUrl, "BaseUrl");
                    }),

                    SuiteHelpers.Case(s, "CustomBaseUrlKeepsSlash", "Validator: custom base URL with trailing slash is unchanged", () =>
                    {
                        MailgunValidator validator = new MailgunValidator(ApiKey, "https://api.eu.mailgun.net/v4/");
                        TestAssert.Equal("https://api.eu.mailgun.net/v4/", validator.BaseUrl, "BaseUrl");
                    }),

                    SuiteHelpers.Case(s, "LoggerSettable", "Validator: Logger can be assigned and cleared", () =>
                    {
                        MailgunValidator validator = new MailgunValidator(ApiKey);
                        Action<string> logger = msg => { };
                        validator.Logger = logger;
                        TestAssert.True(ReferenceEquals(logger, validator.Logger), "Logger was not retained");
                        validator.Logger = null;
                        TestAssert.Null(validator.Logger, "Logger");
                    }),

                    SuiteHelpers.Case(s, "NullApiKeyThrows", "Validator: null API key throws ArgumentNullException", () =>
                        TestAssert.ThrowsArgumentNull(() => new MailgunValidator(null), "apiKey")),

                    SuiteHelpers.Case(s, "EmptyApiKeyThrows", "Validator: empty API key throws ArgumentNullException", () =>
                        TestAssert.ThrowsArgumentNull(() => new MailgunValidator(""), "apiKey")),

                    SuiteHelpers.Case(s, "NullBaseUrlThrows", "Validator: null base URL throws ArgumentNullException", () =>
                        TestAssert.ThrowsArgumentNull(() => new MailgunValidator(ApiKey, null), "baseUrl")),

                    SuiteHelpers.Case(s, "EmptyBaseUrlThrows", "Validator: empty base URL throws ArgumentNullException", () =>
                        TestAssert.ThrowsArgumentNull(() => new MailgunValidator(ApiKey, ""), "baseUrl"))
                });
        }

        /// <summary>
        /// Argument validation for validate methods.
        /// </summary>
        /// <returns>Suite.</returns>
        public static TestSuiteDescriptor ArgumentValidationSuite()
        {
            const string s = "ValidatorArguments";

            return new TestSuiteDescriptor(
                suiteId: s,
                displayName: "MailgunValidator argument validation",
                cases: new List<TestCaseDescriptor>
                {
                    SuiteHelpers.Case(s, "NullAddressThrows", "ValidateAsync: null address throws ArgumentNullException", async ct =>
                        await TestAssert.ThrowsArgumentNullAsync(() => Unused().ValidateAsync(null, ct), "address").ConfigureAwait(false)),

                    SuiteHelpers.Case(s, "EmptyAddressThrows", "ValidateAsync: empty address throws ArgumentNullException", async ct =>
                        await TestAssert.ThrowsArgumentNullAsync(() => Unused().ValidateAsync("", ct), "address").ConfigureAwait(false)),

                    SuiteHelpers.Case(s, "SyncNullAddressThrows", "Validate: null address surfaces ArgumentNullException (wrapped in AggregateException)", () =>
                    {
                        AggregateException e = TestAssert.Throws<AggregateException>(() => Unused().Validate(null));
                        ArgumentNullException? inner = e.InnerException as ArgumentNullException;
                        TestAssert.NotNull(inner, "InnerException as ArgumentNullException");
                        TestAssert.Equal("address", inner!.ParamName, "ParamName");
                    }),

                    SuiteHelpers.Case(s, "ValidationPrecedesNetwork", "ValidateAsync: invalid arguments fail before any HTTP request is made", async ct =>
                    {
                        using (MockMailgunServer server = new MockMailgunServer())
                        {
                            await TestAssert.ThrowsAsync<ArgumentNullException>(() => Validator(server).ValidateAsync("", ct)).ConfigureAwait(false);
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
            const string s = "ValidatorRequest";

            return new TestSuiteDescriptor(
                suiteId: s,
                displayName: "MailgunValidator outbound request",
                cases: new List<TestCaseDescriptor>
                {
                    SuiteHelpers.Case(s, "PostsToValidateEndpoint", "ValidateAsync: POSTs to {baseUrl}address/validate", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            await Validator(server).ValidateAsync("user@example.com", ct).ConfigureAwait(false);
                            RecordedRequest req = server.SingleRequest();
                            TestAssert.Equal("POST", req.Method, "Method");
                            TestAssert.Equal("/v4/address/validate", req.Path, "Path");
                        }
                    }),

                    SuiteHelpers.Case(s, "BaseUrlWithoutSlash", "ValidateAsync: base URL without trailing slash still builds the correct path", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            MailgunValidator validator = new MailgunValidator(ApiKey, server.RootUrl + "custom/v4");
                            await validator.ValidateAsync("user@example.com", ct).ConfigureAwait(false);
                            TestAssert.Equal("/custom/v4/address/validate", server.SingleRequest().Path, "Path");
                        }
                    }),

                    SuiteHelpers.Case(s, "BasicAuth", "ValidateAsync: uses HTTP basic auth with user 'api' and the API key", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            await Validator(server).ValidateAsync("user@example.com", ct).ConfigureAwait(false);
                            TestAssert.Equal("api:" + ApiKey, server.SingleRequest().DecodeBasicAuthorization(), "Decoded authorization");
                        }
                    }),

                    SuiteHelpers.Case(s, "AddressField", "ValidateAsync: sends only the 'address' form field", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            await Validator(server).ValidateAsync("user@example.com", ct).ConfigureAwait(false);
                            Dictionary<string, string> form = server.SingleRequest().Form;
                            TestAssert.Equal(1, form.Count, "Form field count");
                            TestAssert.True(form.TryGetValue("address", out string? address), "address field was not sent");
                            TestAssert.Equal("user@example.com", address, "address");
                        }
                    }),

                    SuiteHelpers.Case(s, "AddressSpecialCharacters", "ValidateAsync: address with '+', '&' and unicode is encoded and round-trips", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        {
                            string address = "first.last+tag&x=1@exämple.com";
                            await Validator(server).ValidateAsync(address, ct).ConfigureAwait(false);
                            server.SingleRequest().Form.TryGetValue("address", out string? sent);
                            TestAssert.Equal(address, sent, "address");
                        }
                    }),

                    SuiteHelpers.Case(s, "MalformedAddressStillSent", "ValidateAsync: syntactically invalid address is passed to Mailgun for evaluation", async ct =>
                    {
                        using (MockMailgunServer server = Respond(200,
                            "{\"address\":\"not-an-address\",\"is_disposable_address\":false,\"is_role_address\":false,"
                            + "\"reason\":[\"failed_syntax_check\"],\"result\":\"undeliverable\",\"risk\":\"high\"}"))
                        {
                            MailgunValidationResult? result = await Validator(server).ValidateAsync("not-an-address", ct).ConfigureAwait(false);
                            TestAssert.NotNull(result, "Result");
                            TestAssert.Equal(ResultEnum.Undeliverable, result!.Result, "Result");
                            TestAssert.Equal(RiskEnum.High, result.Risk, "Risk");
                            TestAssert.Equal("failed_syntax_check", result.Reason[0], "Reason[0]");
                        }
                    })
                });
        }

        /// <summary>
        /// Deserialization of validation responses.
        /// </summary>
        /// <returns>Suite.</returns>
        public static TestSuiteDescriptor ResponseSuite()
        {
            const string s = "ValidatorResponse";

            List<TestCaseDescriptor> cases = new List<TestCaseDescriptor>
            {
                SuiteHelpers.Case(s, "Deliverable", "ValidateAsync: deliverable response is fully deserialized", async ct =>
                {
                    using (MockMailgunServer server = Success())
                    {
                        MailgunValidationResult? result = await Validator(server).ValidateAsync("user@example.com", ct).ConfigureAwait(false);
                        TestAssert.NotNull(result, "Result");
                        TestAssert.Equal("user@example.com", result!.Address, "Address");
                        TestAssert.False(result.IsDisposable, "IsDisposable");
                        TestAssert.False(result.IsRoleAddress, "IsRoleAddress");
                        TestAssert.NotNull(result.Reason, "Reason");
                        TestAssert.Equal(0, result.Reason.Count, "Reason count");
                        TestAssert.Equal(ResultEnum.Deliverable, result.Result, "Result");
                        TestAssert.Equal(RiskEnum.Low, result.Risk, "Risk");
                        TestAssert.Null(result.RootAddress, "RootAddress");
                    }
                }),

                SuiteHelpers.Case(s, "SyncDeliverable", "Validate: synchronous validation returns the deserialized result", () =>
                {
                    using (MockMailgunServer server = Success())
                    {
                        MailgunValidationResult result = Validator(server).Validate("user@example.com");
                        TestAssert.NotNull(result, "Result");
                        TestAssert.Equal(ResultEnum.Deliverable, result.Result, "Result");
                        TestAssert.Equal(RiskEnum.Low, result.Risk, "Risk");
                    }
                }),

                SuiteHelpers.Case(s, "FlagsAndRootAddress", "ValidateAsync: disposable, role, reasons and root_address are deserialized", async ct =>
                {
                    string body = "{\"address\":\"sales+promo@tempmail.example\",\"is_disposable_address\":true,\"is_role_address\":true,"
                        + "\"reason\":[\"mailbox_is_disposable_address\",\"mailbox_is_role_address\"],"
                        + "\"result\":\"undeliverable\",\"risk\":\"high\",\"root_address\":\"sales@tempmail.example\"}";

                    using (MockMailgunServer server = Respond(200, body))
                    {
                        MailgunValidationResult? result = await Validator(server).ValidateAsync("sales+promo@tempmail.example", ct).ConfigureAwait(false);
                        TestAssert.NotNull(result, "Result");
                        TestAssert.True(result!.IsDisposable, "IsDisposable");
                        TestAssert.True(result.IsRoleAddress, "IsRoleAddress");
                        TestAssert.Equal(2, result.Reason.Count, "Reason count");
                        TestAssert.Equal("mailbox_is_disposable_address", result.Reason[0], "Reason[0]");
                        TestAssert.Equal("mailbox_is_role_address", result.Reason[1], "Reason[1]");
                        TestAssert.Equal("sales@tempmail.example", result.RootAddress, "RootAddress");
                    }
                }),

                SuiteHelpers.Case(s, "MinimalResponseDefaults", "ValidateAsync: missing fields fall back to model defaults", async ct =>
                {
                    using (MockMailgunServer server = Respond(200, "{\"address\":\"user@example.com\"}"))
                    {
                        MailgunValidationResult? result = await Validator(server).ValidateAsync("user@example.com", ct).ConfigureAwait(false);
                        TestAssert.NotNull(result, "Result");
                        TestAssert.Equal("user@example.com", result!.Address, "Address");
                        TestAssert.False(result.IsDisposable, "IsDisposable");
                        TestAssert.False(result.IsRoleAddress, "IsRoleAddress");
                        TestAssert.NotNull(result.Reason, "Reason");
                        TestAssert.Equal(ResultEnum.Unknown, result.Result, "Result");
                        TestAssert.Equal(RiskEnum.Unknown, result.Risk, "Risk");
                    }
                }),

                SuiteHelpers.Case(s, "ExtraFieldsIgnored", "ValidateAsync: unknown response fields (e.g. engagement) are ignored", async ct =>
                {
                    string body = "{\"address\":\"user@example.com\",\"is_disposable_address\":false,\"is_role_address\":false,\"reason\":[],"
                        + "\"result\":\"deliverable\",\"risk\":\"low\",\"engagement\":{\"engaging\":false,\"is_bot\":false,\"behavior\":\"disengaged\"},"
                        + "\"did_you_mean\":null}";

                    using (MockMailgunServer server = Respond(200, body))
                    {
                        MailgunValidationResult? result = await Validator(server).ValidateAsync("user@example.com", ct).ConfigureAwait(false);
                        TestAssert.NotNull(result, "Result");
                        TestAssert.Equal(ResultEnum.Deliverable, result!.Result, "Result");
                    }
                }),

                SuiteHelpers.Case(s, "CatchAllDomain", "ValidateAsync: catch-all domain response (result catch_all, risk medium) is deserialized", async ct =>
                {
                    string body = "{\"address\":\"anyone@catchall.example\",\"is_disposable_address\":false,\"is_role_address\":false,"
                        + "\"reason\":[\"catch_all\"],\"result\":\"catch_all\",\"risk\":\"medium\"}";

                    using (MockMailgunServer server = Respond(200, body))
                    {
                        MailgunValidationResult? result = await Validator(server).ValidateAsync("anyone@catchall.example", ct).ConfigureAwait(false);
                        TestAssert.NotNull(result, "Result");
                        TestAssert.Equal(ResultEnum.CatchAll, result!.Result, "Result");
                        TestAssert.Equal(RiskEnum.Medium, result.Risk, "Risk");
                    }
                }),

                SuiteHelpers.Case(s, "UnrecognizedValuesMapToUnknown", "ValidateAsync: unrecognized result/risk strings map to Unknown instead of throwing", async ct =>
                {
                    using (MockMailgunServer server = Respond(200, "{\"address\":\"user@example.com\",\"result\":\"some_future_value\",\"risk\":\"extreme\"}"))
                    {
                        MailgunValidationResult? result = await Validator(server).ValidateAsync("user@example.com", ct).ConfigureAwait(false);
                        TestAssert.NotNull(result, "Result");
                        TestAssert.Equal(ResultEnum.Unknown, result!.Result, "Result");
                        TestAssert.Equal(RiskEnum.Unknown, result.Risk, "Risk");
                    }
                }),

                SuiteHelpers.Case(s, "NullValuesMapToUnknown", "ValidateAsync: null result/risk map to Unknown", async ct =>
                {
                    using (MockMailgunServer server = Respond(200, "{\"address\":\"user@example.com\",\"result\":null,\"risk\":null}"))
                    {
                        MailgunValidationResult? result = await Validator(server).ValidateAsync("user@example.com", ct).ConfigureAwait(false);
                        TestAssert.NotNull(result, "Result");
                        TestAssert.Equal(ResultEnum.Unknown, result!.Result, "Result");
                        TestAssert.Equal(RiskEnum.Unknown, result.Risk, "Risk");
                    }
                }),

                SuiteHelpers.Case(s, "EmptyBodyReturnsNull", "ValidateAsync: 200 with an empty body returns null", async ct =>
                    await AssertValidateReturnsNull(200, "", ct).ConfigureAwait(false)),

                SuiteHelpers.Case(s, "Unauthorized401ReturnsNull", "ValidateAsync: 401 Unauthorized returns null", async ct =>
                    await AssertValidateReturnsNull(401, "Forbidden", ct).ConfigureAwait(false)),

                SuiteHelpers.Case(s, "BadRequest400ReturnsNull", "ValidateAsync: 400 Bad Request returns null", async ct =>
                    await AssertValidateReturnsNull(400, "{\"message\":\"Missing parameter address\"}", ct).ConfigureAwait(false)),

                SuiteHelpers.Case(s, "TooManyRequests429ReturnsNull", "ValidateAsync: 429 Too Many Requests returns null", async ct =>
                    await AssertValidateReturnsNull(429, "{\"message\":\"Too many requests\"}", ct).ConfigureAwait(false)),

                SuiteHelpers.Case(s, "ServerError500ReturnsNull", "ValidateAsync: 500 Internal Server Error returns null", async ct =>
                    await AssertValidateReturnsNull(500, "{\"message\":\"Internal error\"}", ct).ConfigureAwait(false)),

                SuiteHelpers.Case(s, "LoggerReceivesMessages", "ValidateAsync: Logger receives URL, status, address, result and risk", async ct =>
                {
                    using (MockMailgunServer server = Success())
                    {
                        List<string> logs = new List<string>();
                        MailgunValidator validator = Validator(server);
                        validator.Logger = msg => { lock (logs) { logs.Add(msg); } };
                        await validator.ValidateAsync("user@example.com", ct).ConfigureAwait(false);

                        string all = String.Join("\n", logs);
                        TestAssert.True(logs.TrueForAll(m => m.StartsWith("[MailgunValidator] ", StringComparison.Ordinal)), "All log lines should carry the [MailgunValidator] header");
                        TestAssert.True(all.Contains(server.RootUrl + "v4/address/validate"), "Log should contain the request URL");
                        TestAssert.True(all.Contains("200"), "Log should contain the status code");
                        TestAssert.True(all.Contains("user@example.com"), "Log should contain the address");
                        TestAssert.True(all.Contains("Deliverable") && all.Contains("Low"), "Log should contain the result and risk");
                    }
                })
            };

            foreach (KeyValuePair<string, ResultEnum> kvp in ResultWireValues())
            {
                string wire = kvp.Key;
                ResultEnum expected = kvp.Value;

                cases.Add(SuiteHelpers.Case(s, "Result_" + wire, "ValidateAsync: result \"" + wire + "\" maps to ResultEnum." + expected, async ct =>
                {
                    using (MockMailgunServer server = Respond(200, "{\"address\":\"user@example.com\",\"result\":\"" + wire + "\",\"risk\":\"unknown\"}"))
                    {
                        MailgunValidationResult? result = await Validator(server).ValidateAsync("user@example.com", ct).ConfigureAwait(false);
                        TestAssert.NotNull(result, "Result");
                        TestAssert.Equal(expected, result!.Result, "Result");
                    }
                }));
            }

            foreach (KeyValuePair<string, RiskEnum> kvp in RiskWireValues())
            {
                string wire = kvp.Key;
                RiskEnum expected = kvp.Value;

                cases.Add(SuiteHelpers.Case(s, "Risk_" + wire, "ValidateAsync: risk \"" + wire + "\" maps to RiskEnum." + expected, async ct =>
                {
                    using (MockMailgunServer server = Respond(200, "{\"address\":\"user@example.com\",\"result\":\"unknown\",\"risk\":\"" + wire + "\"}"))
                    {
                        MailgunValidationResult? result = await Validator(server).ValidateAsync("user@example.com", ct).ConfigureAwait(false);
                        TestAssert.NotNull(result, "Result");
                        TestAssert.Equal(expected, result!.Risk, "Risk");
                    }
                }));
            }

            return new TestSuiteDescriptor(
                suiteId: s,
                displayName: "MailgunValidator response handling",
                cases: cases);
        }

        /// <summary>
        /// Exceptional conditions: malformed responses, network failure, cancellation.
        /// </summary>
        /// <returns>Suite.</returns>
        public static TestSuiteDescriptor FailureSuite()
        {
            const string s = "ValidatorFailure";

            return new TestSuiteDescriptor(
                suiteId: s,
                displayName: "MailgunValidator failure handling",
                cases: new List<TestCaseDescriptor>
                {
                    SuiteHelpers.Case(s, "InvalidJsonThrows", "ValidateAsync: 200 with invalid JSON throws JsonException with context data", async ct =>
                    {
                        using (MockMailgunServer server = Respond(200, "<html>not json</html>"))
                        {
                            JsonException e = await TestAssert.ThrowsAsync<JsonException>(() =>
                                Validator(server).ValidateAsync("user@example.com", ct)).ConfigureAwait(false);

                            TestAssert.Equal(server.RootUrl + "v4/address/validate", e.Data["Url"] as string, "Data[Url]");
                            TestAssert.Equal("user@example.com", e.Data["Address"] as string, "Data[Address]");
                            TestAssert.Equal((object)200, e.Data["StatusCode"], "Data[StatusCode]");
                            TestAssert.Equal("<html>not json</html>", e.Data["Response"] as string, "Data[Response]");
                        }
                    }),

                    SuiteHelpers.Case(s, "WrongTypeThrows", "ValidateAsync: 200 with a wrongly-typed field throws JsonException", async ct =>
                    {
                        using (MockMailgunServer server = Respond(200, "{\"address\":\"user@example.com\",\"is_disposable_address\":\"maybe\"}"))
                        {
                            await TestAssert.ThrowsAsync<JsonException>(() =>
                                Validator(server).ValidateAsync("user@example.com", ct)).ConfigureAwait(false);
                        }
                    }),

                    SuiteHelpers.Case(s, "ConnectionRefusedThrows", "ValidateAsync: unreachable server throws HttpRequestException with context data", async ct =>
                    {
                        string url = "http://127.0.0.1:" + MockMailgunServer.GetFreePort() + "/v4/";
                        MailgunValidator validator = new MailgunValidator(ApiKey, url);
                        HttpRequestException e = await TestAssert.ThrowsAsync<HttpRequestException>(() =>
                            validator.ValidateAsync("user@example.com", ct)).ConfigureAwait(false);

                        TestAssert.Equal(url + "address/validate", e.Data["Url"] as string, "Data[Url]");
                        TestAssert.Equal("user@example.com", e.Data["Address"] as string, "Data[Address]");
                        TestAssert.False(e.Data.Contains("StatusCode"), "Data should not contain StatusCode when no response was received");
                    }),

                    SuiteHelpers.Case(s, "PreCancelledThrows", "ValidateAsync: already-cancelled token throws OperationCanceledException without sending", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        using (CancellationTokenSource cts = new CancellationTokenSource())
                        {
                            cts.Cancel();
                            await TestAssert.ThrowsAsync<OperationCanceledException>(() =>
                                Validator(server).ValidateAsync("user@example.com", cts.Token)).ConfigureAwait(false);
                            TestAssert.Equal(0, server.Requests.Count, "Request count");
                        }
                    }),

                    SuiteHelpers.Case(s, "CancelledInFlightThrows", "ValidateAsync: cancellation while awaiting the response throws OperationCanceledException", async ct =>
                    {
                        using (MockMailgunServer server = Success())
                        using (CancellationTokenSource cts = new CancellationTokenSource())
                        {
                            server.ResponseDelayMs = 5000;
                            cts.CancelAfter(250);
                            await TestAssert.ThrowsAsync<OperationCanceledException>(() =>
                                Validator(server).ValidateAsync("user@example.com", cts.Token)).ConfigureAwait(false);
                        }
                    })
                });
        }

        #endregion

        #region Private-Methods

        private static Dictionary<string, ResultEnum> ResultWireValues()
        {
            return new Dictionary<string, ResultEnum>
            {
                { "deliverable", ResultEnum.Deliverable },
                { "undeliverable", ResultEnum.Undeliverable },
                { "do_not_send", ResultEnum.DoNotSend },
                { "catch_all", ResultEnum.CatchAll },
                { "unknown", ResultEnum.Unknown },
                { "DO_NOT_SEND", ResultEnum.DoNotSend },
                { "DoNotSend", ResultEnum.DoNotSend },
                { "CatchAll", ResultEnum.CatchAll },
                { "catch-all", ResultEnum.CatchAll },
                { "Deliverable", ResultEnum.Deliverable }
            };
        }

        private static Dictionary<string, RiskEnum> RiskWireValues()
        {
            return new Dictionary<string, RiskEnum>
            {
                { "high", RiskEnum.High },
                { "medium", RiskEnum.Medium },
                { "low", RiskEnum.Low },
                { "unknown", RiskEnum.Unknown },
                { "HIGH", RiskEnum.High },
                { "Medium", RiskEnum.Medium }
            };
        }

        private static MailgunValidator Unused()
        {
            return new MailgunValidator(ApiKey, "http://127.0.0.1:1/v4/");
        }

        private static MailgunValidator Validator(MockMailgunServer server)
        {
            return new MailgunValidator(ApiKey, server.RootUrl + "v4/");
        }

        private static MockMailgunServer Success()
        {
            return Respond(200, DeliverableBody);
        }

        private static MockMailgunServer Respond(int statusCode, string body)
        {
            MockMailgunServer server = new MockMailgunServer();
            server.StatusCode = statusCode;
            server.ResponseBody = body;
            return server;
        }

        private static async Task AssertValidateReturnsNull(int statusCode, string body, CancellationToken ct)
        {
            using (MockMailgunServer server = Respond(statusCode, body))
            {
                MailgunValidationResult? result = await Validator(server).ValidateAsync("user@example.com", ct).ConfigureAwait(false);
                TestAssert.Null(result, "Result for HTTP " + statusCode);
                TestAssert.Equal(1, server.Requests.Count, "Request count");
            }
        }

        #endregion
    }
}
