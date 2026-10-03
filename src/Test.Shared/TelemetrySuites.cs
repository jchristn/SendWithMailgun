namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using SendWithMailgun;
    using Touchstone.Core;

    /// <summary>
    /// Shared suites proving that SendWithMailgun emits its documented metrics and spans.
    /// </summary>
    public static class TelemetrySuites
    {
        #region Public-Methods

        /// <summary>
        /// Telemetry contract: names, instruments, build info, and the no-listener path.
        /// </summary>
        /// <returns>Suite.</returns>
        public static TestSuiteDescriptor ContractSuite()
        {
            const string s = "TelemetryContract";

            return new TestSuiteDescriptor(
                suiteId: s,
                displayName: "Telemetry contract",
                cases: new List<TestCaseDescriptor>
                {
                    SuiteHelpers.Case(s, "SourceNames", "Telemetry: meter and activity source are named SendWithMailgun", () =>
                    {
                        TestAssert.Equal("SendWithMailgun", MailgunTelemetry.MeterName, "MeterName");
                        TestAssert.Equal("SendWithMailgun", MailgunTelemetry.ActivitySourceName, "ActivitySourceName");
                    }),

                    SuiteHelpers.Case(s, "InstrumentsPublished", "Telemetry: every documented instrument is published on the meter", () =>
                    {
                        MailgunSender unused = new MailgunSender(SenderSuites.Domain, SenderSuites.ApiKey);

                        using (TelemetryCapture capture = new TelemetryCapture())
                        {
                            List<string> published = capture.PublishedInstruments;
                            string[] expected = new string[]
                            {
                                MailgunTelemetry.MetricOperationDuration,
                                MailgunTelemetry.MetricOperations,
                                MailgunTelemetry.MetricActiveOperations,
                                MailgunTelemetry.MetricStageDuration,
                                MailgunTelemetry.MetricSendRecipients,
                                MailgunTelemetry.MetricSendBodySize,
                                MailgunTelemetry.MetricValidationResults,
                                MailgunTelemetry.MetricBuildInfo
                            };

                            foreach (string name in expected)
                                TestAssert.True(published.Contains(name), "Instrument not published: " + name);
                        }
                    }),

                    SuiteHelpers.Case(s, "BuildInfo", "Telemetry: build-info gauge reports 1 with the library version", () =>
                    {
                        MailgunSender unused = new MailgunSender(SenderSuites.Domain, SenderSuites.ApiKey);

                        using (TelemetryCapture capture = new TelemetryCapture())
                        {
                            capture.RecordObservables();
                            RecordedMeasurement? m = capture.Measurements.FirstOrDefault(x => x.Instrument == MailgunTelemetry.MetricBuildInfo);
                            TestAssert.NotNull(m, "Build info measurement");
                            TestAssert.Equal(1d, m!.Value, "Build info value");
                            string? version = m.Tag(MailgunTelemetry.AttributeVersion);
                            TestAssert.True(!String.IsNullOrEmpty(version) && version != "unknown", "Version label: " + version);
                            TestAssert.False(version!.Contains("+"), "Version label should not carry source revision: " + version);
                        }
                    }),

                    SuiteHelpers.Case(s, "Units", "Telemetry: durations are in seconds and body size in bytes", async ct =>
                    {
                        using (MockMailgunServer server = Respond(200, SenderSuites.SuccessBody))
                        using (TelemetryCapture capture = new TelemetryCapture())
                        {
                            await Sender(server).SendAsync("to@example.com", "from@example.com", "Subject", "Body", token: ct).ConfigureAwait(false);
                            List<RecordedMeasurement> all = capture.Measurements;
                            TestAssert.Equal("s", Find(capture.ForPort(Port(server)), MailgunTelemetry.MetricOperationDuration).Unit, "Operation duration unit");
                            TestAssert.Equal("s", all.First(m => m.Instrument == MailgunTelemetry.MetricStageDuration).Unit, "Stage duration unit");
                            TestAssert.Equal("By", all.First(m => m.Instrument == MailgunTelemetry.MetricSendBodySize).Unit, "Body size unit");
                        }
                    }),

                    SuiteHelpers.Case(s, "NoListener", "Telemetry: operations succeed with no listener attached", async ct =>
                    {
                        using (MockMailgunServer server = Respond(200, SenderSuites.SuccessBody))
                        {
                            string id = await Sender(server).SendAsync("to@example.com", "from@example.com", "Subject", "Body", token: ct).ConfigureAwait(false);
                            TestAssert.Equal(SenderSuites.SuccessId, id, "Message ID");
                        }

                        using (MockMailgunServer server = Respond(500, ""))
                        {
                            string id = await Sender(server).SendAsync("to@example.com", "from@example.com", "Subject", "Body", token: ct).ConfigureAwait(false);
                            TestAssert.Null(id, "Message ID");
                        }

                        using (MockMailgunServer server = Respond(200, ValidatorSuites.DeliverableBody))
                        {
                            MailgunValidationResult result = await Validator(server).ValidateAsync("user@example.com", ct).ConfigureAwait(false);
                            TestAssert.NotNull(result, "Validation result");
                        }
                    })
                });
        }

        /// <summary>
        /// Telemetry emitted by <see cref="MailgunSender"/>.
        /// </summary>
        /// <returns>Suite.</returns>
        public static TestSuiteDescriptor SenderSuite()
        {
            const string s = "TelemetrySender";

            return new TestSuiteDescriptor(
                suiteId: s,
                displayName: "Telemetry: MailgunSender",
                cases: new List<TestCaseDescriptor>
                {
                    SuiteHelpers.Case(s, "SuccessSpan", "Send telemetry: success emits a client span with stage children and Ok status", async ct =>
                    {
                        using (MockMailgunServer server = Respond(200, SenderSuites.SuccessBody))
                        using (TelemetryCapture capture = new TelemetryCapture())
                        {
                            await Sender(server).SendAsync("a@example.com, b@example.com", "from@example.com", "Secret subject", "Secret body", true, "c@example.com", token: ct).ConfigureAwait(false);

                            Activity op = capture.SingleSpan(MailgunTelemetry.SpanSend);
                            TestAssert.Equal(ActivityKind.Client, op.Kind, "Kind");
                            TestAssert.Equal(ActivityStatusCode.Ok, op.Status, "Status");
                            TestAssert.Equal(capture.Root!.SpanId, op.ParentSpanId, "Parent span");
                            TestAssert.Equal(MailgunTelemetry.OperationSend, Tag(op, MailgunTelemetry.AttributeOperation), "Operation tag");
                            TestAssert.Equal(MailgunTelemetry.OutcomeSuccess, Tag(op, MailgunTelemetry.AttributeOutcome), "Outcome tag");
                            TestAssert.Equal(SenderSuites.Domain, Tag(op, MailgunTelemetry.AttributeDomain), "Domain tag");
                            TestAssert.Equal(SenderSuites.SuccessId, Tag(op, MailgunTelemetry.AttributeMessageId), "Message ID tag");
                            TestAssert.Equal("200", Tag(op, MailgunTelemetry.AttributeHttpStatusCode), "Status code tag");
                            TestAssert.Equal("3", Tag(op, MailgunTelemetry.AttributeRecipientCount), "Recipient count tag");
                            TestAssert.Equal(MailgunTelemetry.BodyFormatHtml, Tag(op, MailgunTelemetry.AttributeBodyFormat), "Body format tag");
                            TestAssert.Equal("127.0.0.1", Tag(op, MailgunTelemetry.AttributeServerAddress), "Server address tag");

                            Activity request = capture.SingleSpan(MailgunTelemetry.SpanStagePrefix + MailgunTelemetry.StageRequest);
                            Activity deserialize = capture.SingleSpan(MailgunTelemetry.SpanStagePrefix + MailgunTelemetry.StageDeserialize);
                            TestAssert.Equal(op.SpanId, request.ParentSpanId, "Request stage parent");
                            TestAssert.Equal(op.SpanId, deserialize.ParentSpanId, "Deserialize stage parent");
                            TestAssert.Equal(ActivityStatusCode.Ok, request.Status, "Request stage status");
                            TestAssert.Equal(ActivityStatusCode.Ok, deserialize.Status, "Deserialize stage status");
                        }
                    }),

                    SuiteHelpers.Case(s, "SuccessMetrics", "Send telemetry: success records operation, stage, active, recipient, and body-size metrics", async ct =>
                    {
                        using (MockMailgunServer server = Respond(200, SenderSuites.SuccessBody))
                        using (TelemetryCapture capture = new TelemetryCapture())
                        {
                            await Sender(server).SendAsync("a@example.com,b@example.com", "from@example.com", "Subject", "Hello", false, "c@example.com", "d@example.com", ct).ConfigureAwait(false);

                            List<RecordedMeasurement> mine = capture.ForPort(Port(server));
                            RecordedMeasurement count = Find(mine, MailgunTelemetry.MetricOperations);
                            TestAssert.Equal(1d, count.Value, "Operations count");
                            TestAssert.Equal(MailgunTelemetry.OperationSend, count.Tag(MailgunTelemetry.AttributeOperation), "Operation label");
                            TestAssert.Equal(MailgunTelemetry.OutcomeSuccess, count.Tag(MailgunTelemetry.AttributeOutcome), "Outcome label");
                            TestAssert.Equal("200", count.Tag(MailgunTelemetry.AttributeHttpStatusCode), "Status label");
                            TestAssert.Null(count.Tag(MailgunTelemetry.AttributeErrorType), "error.type on success");

                            RecordedMeasurement duration = Find(mine, MailgunTelemetry.MetricOperationDuration);
                            TestAssert.True(duration.Value > 0, "Duration should be positive");

                            List<RecordedMeasurement> active = mine.Where(m => m.Instrument == MailgunTelemetry.MetricActiveOperations).ToList();
                            TestAssert.Equal(2, active.Count, "Active operation measurements");
                            TestAssert.Equal(0d, active.Sum(m => m.Value), "Active operations net");

                            List<string> stages = capture.Measurements
                                .Where(m => m.Instrument == MailgunTelemetry.MetricStageDuration
                                    && m.Tag(MailgunTelemetry.AttributeOperation) == MailgunTelemetry.OperationSend
                                    && m.Tag(MailgunTelemetry.AttributeStageOutcome) == MailgunTelemetry.OutcomeSuccess)
                                .Select(m => m.Tag(MailgunTelemetry.AttributeStage) ?? "")
                                .ToList();
                            TestAssert.True(stages.Contains(MailgunTelemetry.StageRequest), "Request stage duration recorded");
                            TestAssert.True(stages.Contains(MailgunTelemetry.StageDeserialize), "Deserialize stage duration recorded");

                            TestAssert.True(capture.Measurements.Any(m => m.Instrument == MailgunTelemetry.MetricSendRecipients && m.Value == 4d
                                && m.Tag(MailgunTelemetry.AttributeBodyFormat) == MailgunTelemetry.BodyFormatText), "Recipients histogram value 4 (text)");
                            TestAssert.True(capture.Measurements.Any(m => m.Instrument == MailgunTelemetry.MetricSendBodySize && m.Value == 5d), "Body size histogram value 5");
                        }
                    }),

                    SuiteHelpers.Case(s, "HttpError", "Send telemetry: non-200 records http_error with the status code as error.type", async ct =>
                    {
                        using (MockMailgunServer server = Respond(401, "{\"message\":\"Forbidden\"}"))
                        using (TelemetryCapture capture = new TelemetryCapture())
                        {
                            string id = await Sender(server).SendAsync("to@example.com", "from@example.com", "Subject", "Body", token: ct).ConfigureAwait(false);
                            TestAssert.Null(id, "Message ID");
                            AssertOutcome(capture, server, MailgunTelemetry.SpanSend, MailgunTelemetry.OutcomeHttpError, "401", "401");
                        }
                    }),

                    SuiteHelpers.Case(s, "EmptyResponse", "Send telemetry: 200 with empty body records empty_response", async ct =>
                    {
                        using (MockMailgunServer server = Respond(200, ""))
                        using (TelemetryCapture capture = new TelemetryCapture())
                        {
                            await Sender(server).SendAsync("to@example.com", "from@example.com", "Subject", "Body", token: ct).ConfigureAwait(false);
                            AssertOutcome(capture, server, MailgunTelemetry.SpanSend, MailgunTelemetry.OutcomeEmptyResponse, MailgunTelemetry.OutcomeEmptyResponse, "200");
                        }
                    }),

                    SuiteHelpers.Case(s, "MalformedResponse", "Send telemetry: 200 without an id records malformed_response", async ct =>
                    {
                        using (MockMailgunServer server = Respond(200, "{\"message\":\"Queued\"}"))
                        using (TelemetryCapture capture = new TelemetryCapture())
                        {
                            string id = await Sender(server).SendAsync("to@example.com", "from@example.com", "Subject", "Body", token: ct).ConfigureAwait(false);
                            TestAssert.Null(id, "Message ID");
                            AssertOutcome(capture, server, MailgunTelemetry.SpanSend, MailgunTelemetry.OutcomeMalformedResponse, MailgunTelemetry.OutcomeMalformedResponse, "200");
                        }
                    }),

                    SuiteHelpers.Case(s, "InvalidJson", "Send telemetry: unparseable body fails the deserialize stage and records the exception", async ct =>
                    {
                        using (MockMailgunServer server = Respond(200, "not json"))
                        using (TelemetryCapture capture = new TelemetryCapture())
                        {
                            Exception e = await TestAssert.ThrowsAsync<Exception>(() => Sender(server).SendAsync("to@example.com", "from@example.com", "Subject", "Body", token: ct)).ConfigureAwait(false);
                            string errorType = e.GetType().FullName!;
                            AssertOutcome(capture, server, MailgunTelemetry.SpanSend, MailgunTelemetry.OutcomeError, errorType, "200");

                            Activity op = capture.SingleSpan(MailgunTelemetry.SpanSend);
                            TestAssert.True(op.Events.Any(ev => ev.Name == "exception"), "Exception event on operation span");

                            Activity stage = capture.SingleSpan(MailgunTelemetry.SpanStagePrefix + MailgunTelemetry.StageDeserialize);
                            TestAssert.Equal(ActivityStatusCode.Error, stage.Status, "Deserialize stage status");
                            TestAssert.True(capture.Measurements.Any(m => m.Instrument == MailgunTelemetry.MetricStageDuration
                                && m.Tag(MailgunTelemetry.AttributeStage) == MailgunTelemetry.StageDeserialize
                                && m.Tag(MailgunTelemetry.AttributeStageOutcome) == MailgunTelemetry.OutcomeError), "Failed deserialize stage metric");
                        }
                    }),

                    SuiteHelpers.Case(s, "ConnectionRefused", "Send telemetry: connection failure fails the request stage and records error with exception type", async ct =>
                    {
                        int port = MockMailgunServer.GetFreePort();
                        MailgunSender sender = new MailgunSender(SenderSuites.Domain, SenderSuites.ApiKey, "http://127.0.0.1:" + port + "/v3/");

                        using (TelemetryCapture capture = new TelemetryCapture())
                        {
                            Exception e = await TestAssert.ThrowsAsync<Exception>(() => sender.SendAsync("to@example.com", "from@example.com", "Subject", "Body", token: ct)).ConfigureAwait(false);

                            Activity op = capture.SingleSpan(MailgunTelemetry.SpanSend);
                            TestAssert.Equal(ActivityStatusCode.Error, op.Status, "Status");
                            TestAssert.Equal(MailgunTelemetry.OutcomeError, Tag(op, MailgunTelemetry.AttributeOutcome), "Outcome tag");
                            TestAssert.Equal(e.GetType().FullName, Tag(op, MailgunTelemetry.AttributeErrorType), "error.type tag");
                            TestAssert.True(op.Events.Any(ev => ev.Name == "exception"), "Exception event");

                            Activity stage = capture.SingleSpan(MailgunTelemetry.SpanStagePrefix + MailgunTelemetry.StageRequest);
                            TestAssert.Equal(ActivityStatusCode.Error, stage.Status, "Request stage status");

                            RecordedMeasurement count = Find(capture.ForPort(port), MailgunTelemetry.MetricOperations);
                            TestAssert.Equal(MailgunTelemetry.OutcomeError, count.Tag(MailgunTelemetry.AttributeOutcome), "Outcome label");
                            TestAssert.Equal(e.GetType().FullName, count.Tag(MailgunTelemetry.AttributeErrorType), "error.type label");
                            TestAssert.Null(count.Tag(MailgunTelemetry.AttributeHttpStatusCode), "Status label without a response");
                        }
                    }),

                    SuiteHelpers.Case(s, "Cancelled", "Send telemetry: caller cancellation records cancelled", async ct =>
                    {
                        using (MockMailgunServer server = Respond(200, SenderSuites.SuccessBody))
                        using (TelemetryCapture capture = new TelemetryCapture())
                        using (CancellationTokenSource cts = new CancellationTokenSource())
                        {
                            cts.Cancel();
                            await TestAssert.ThrowsAsync<OperationCanceledException>(() => Sender(server).SendAsync("to@example.com", "from@example.com", "Subject", "Body", token: cts.Token)).ConfigureAwait(false);

                            Activity op = capture.SingleSpan(MailgunTelemetry.SpanSend);
                            TestAssert.Equal(MailgunTelemetry.OutcomeCancelled, Tag(op, MailgunTelemetry.AttributeOutcome), "Outcome tag");
                            TestAssert.Equal(ActivityStatusCode.Error, op.Status, "Status");
                            RecordedMeasurement count = Find(capture.ForPort(Port(server)), MailgunTelemetry.MetricOperations);
                            TestAssert.Equal(MailgunTelemetry.OutcomeCancelled, count.Tag(MailgunTelemetry.AttributeOutcome), "Outcome label");
                        }
                    }),

                    SuiteHelpers.Case(s, "TraceContextPropagated", "Send telemetry: outbound request carries a W3C traceparent in the caller's trace", async ct =>
                    {
                        using (MockMailgunServer server = Respond(200, SenderSuites.SuccessBody))
                        using (TelemetryCapture capture = new TelemetryCapture())
                        {
                            await Sender(server).SendAsync("to@example.com", "from@example.com", "Subject", "Body", token: ct).ConfigureAwait(false);
                            string? traceparent = server.SingleRequest().TraceParent;
                            TestAssert.NotNull(traceparent, "traceparent header");
                            TestAssert.True(traceparent!.Contains(capture.Root!.TraceId.ToHexString()), "traceparent trace id: " + traceparent);
                        }
                    }),

                    SuiteHelpers.Case(s, "NoSensitiveData", "Send telemetry: no addresses, subject, body, or API key appear in spans or metrics", async ct =>
                    {
                        using (MockMailgunServer server = Respond(200, SenderSuites.SuccessBody))
                        using (TelemetryCapture capture = new TelemetryCapture())
                        {
                            await Sender(server).SendAsync("secret-to@example.com", "secret-from@example.com", "Secret subject", "Secret body", false, "secret-cc@example.com", "secret-bcc@example.com", ct).ConfigureAwait(false);
                            AssertNoSensitiveData(capture, new string[] { "secret-to", "secret-from", "secret-cc", "secret-bcc", "Secret subject", "Secret body", SenderSuites.ApiKey });
                        }
                    })
                });
        }

        /// <summary>
        /// Telemetry emitted by <see cref="MailgunValidator"/>.
        /// </summary>
        /// <returns>Suite.</returns>
        public static TestSuiteDescriptor ValidatorSuite()
        {
            const string s = "TelemetryValidator";

            return new TestSuiteDescriptor(
                suiteId: s,
                displayName: "Telemetry: MailgunValidator",
                cases: new List<TestCaseDescriptor>
                {
                    SuiteHelpers.Case(s, "SuccessSpanAndMetrics", "Validate telemetry: success emits span, stages, outcome, and verdict counter", async ct =>
                    {
                        using (MockMailgunServer server = Respond(200, ValidatorSuites.DeliverableBody))
                        using (TelemetryCapture capture = new TelemetryCapture())
                        {
                            await Validator(server).ValidateAsync("user@example.com", ct).ConfigureAwait(false);

                            Activity op = capture.SingleSpan(MailgunTelemetry.SpanValidate);
                            TestAssert.Equal(ActivityKind.Client, op.Kind, "Kind");
                            TestAssert.Equal(ActivityStatusCode.Ok, op.Status, "Status");
                            TestAssert.Equal("Deliverable", Tag(op, MailgunTelemetry.AttributeValidationResult), "Result tag");
                            TestAssert.Equal("Low", Tag(op, MailgunTelemetry.AttributeValidationRisk), "Risk tag");
                            capture.SingleSpan(MailgunTelemetry.SpanStagePrefix + MailgunTelemetry.StageRequest);
                            capture.SingleSpan(MailgunTelemetry.SpanStagePrefix + MailgunTelemetry.StageDeserialize);

                            RecordedMeasurement count = Find(capture.ForPort(Port(server)), MailgunTelemetry.MetricOperations);
                            TestAssert.Equal(MailgunTelemetry.OperationValidate, count.Tag(MailgunTelemetry.AttributeOperation), "Operation label");
                            TestAssert.Equal(MailgunTelemetry.OutcomeSuccess, count.Tag(MailgunTelemetry.AttributeOutcome), "Outcome label");

                            TestAssert.True(capture.Measurements.Any(m => m.Instrument == MailgunTelemetry.MetricValidationResults
                                && m.Tag(MailgunTelemetry.AttributeValidationResult) == "Deliverable"
                                && m.Tag(MailgunTelemetry.AttributeValidationRisk) == "Low"), "Validation results counter");

                            AssertNoSensitiveData(capture, new string[] { "user@example.com", ValidatorSuites.ApiKey });
                        }
                    }),

                    SuiteHelpers.Case(s, "HttpError", "Validate telemetry: non-200 records http_error", async ct =>
                    {
                        using (MockMailgunServer server = Respond(500, "oops"))
                        using (TelemetryCapture capture = new TelemetryCapture())
                        {
                            MailgunValidationResult result = await Validator(server).ValidateAsync("user@example.com", ct).ConfigureAwait(false);
                            TestAssert.Null(result, "Result");
                            AssertOutcome(capture, server, MailgunTelemetry.SpanValidate, MailgunTelemetry.OutcomeHttpError, "500", "500");
                        }
                    }),

                    SuiteHelpers.Case(s, "EmptyResponse", "Validate telemetry: 200 with empty body records empty_response", async ct =>
                    {
                        using (MockMailgunServer server = Respond(200, ""))
                        using (TelemetryCapture capture = new TelemetryCapture())
                        {
                            await Validator(server).ValidateAsync("user@example.com", ct).ConfigureAwait(false);
                            AssertOutcome(capture, server, MailgunTelemetry.SpanValidate, MailgunTelemetry.OutcomeEmptyResponse, MailgunTelemetry.OutcomeEmptyResponse, "200");
                        }
                    }),

                    SuiteHelpers.Case(s, "InvalidJson", "Validate telemetry: unparseable body records error", async ct =>
                    {
                        using (MockMailgunServer server = Respond(200, "not json"))
                        using (TelemetryCapture capture = new TelemetryCapture())
                        {
                            Exception e = await TestAssert.ThrowsAsync<Exception>(() => Validator(server).ValidateAsync("user@example.com", ct)).ConfigureAwait(false);
                            AssertOutcome(capture, server, MailgunTelemetry.SpanValidate, MailgunTelemetry.OutcomeError, e.GetType().FullName!, "200");
                        }
                    }),

                    SuiteHelpers.Case(s, "ConnectionRefused", "Validate telemetry: connection failure records error", async ct =>
                    {
                        int port = MockMailgunServer.GetFreePort();
                        MailgunValidator validator = new MailgunValidator(ValidatorSuites.ApiKey, "http://127.0.0.1:" + port + "/v4/");

                        using (TelemetryCapture capture = new TelemetryCapture())
                        {
                            Exception e = await TestAssert.ThrowsAsync<Exception>(() => validator.ValidateAsync("user@example.com", ct)).ConfigureAwait(false);
                            Activity op = capture.SingleSpan(MailgunTelemetry.SpanValidate);
                            TestAssert.Equal(ActivityStatusCode.Error, op.Status, "Status");
                            RecordedMeasurement count = Find(capture.ForPort(port), MailgunTelemetry.MetricOperations);
                            TestAssert.Equal(MailgunTelemetry.OutcomeError, count.Tag(MailgunTelemetry.AttributeOutcome), "Outcome label");
                            TestAssert.Equal(e.GetType().FullName, count.Tag(MailgunTelemetry.AttributeErrorType), "error.type label");
                        }
                    })
                });
        }

        #endregion

        #region Private-Methods

        private static MailgunSender Sender(MockMailgunServer server)
        {
            return new MailgunSender(SenderSuites.Domain, SenderSuites.ApiKey, server.RootUrl + "v3/");
        }

        private static MailgunValidator Validator(MockMailgunServer server)
        {
            return new MailgunValidator(ValidatorSuites.ApiKey, server.RootUrl + "v4/");
        }

        private static MockMailgunServer Respond(int statusCode, string body)
        {
            MockMailgunServer server = new MockMailgunServer();
            server.StatusCode = statusCode;
            server.ResponseBody = body;
            return server;
        }

        private static int Port(MockMailgunServer server)
        {
            return new Uri(server.RootUrl).Port;
        }

        private static string? Tag(Activity activity, string key)
        {
            object? val = activity.GetTagItem(key);
            return val == null ? null : Convert.ToString(val, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static RecordedMeasurement Find(List<RecordedMeasurement> measurements, string instrument)
        {
            List<RecordedMeasurement> matches = measurements.Where(m => m.Instrument == instrument).ToList();
            if (matches.Count != 1)
                throw new TestAssertionException("Expected exactly one '" + instrument + "' measurement but found " + matches.Count
                    + ": " + String.Join(" | ", matches));
            return matches[0];
        }

        private static void AssertOutcome(TelemetryCapture capture, MockMailgunServer server, string spanName, string outcome, string errorType, string statusCode)
        {
            Activity op = capture.SingleSpan(spanName);
            TestAssert.Equal(ActivityStatusCode.Error, op.Status, "Span status");
            TestAssert.Equal(outcome, Tag(op, MailgunTelemetry.AttributeOutcome), "Outcome tag");
            TestAssert.Equal(errorType, Tag(op, MailgunTelemetry.AttributeErrorType), "error.type tag");

            List<RecordedMeasurement> mine = capture.ForPort(Port(server));
            RecordedMeasurement count = Find(mine, MailgunTelemetry.MetricOperations);
            TestAssert.Equal(outcome, count.Tag(MailgunTelemetry.AttributeOutcome), "Outcome label");
            TestAssert.Equal(errorType, count.Tag(MailgunTelemetry.AttributeErrorType), "error.type label");
            TestAssert.Equal(statusCode, count.Tag(MailgunTelemetry.AttributeHttpStatusCode), "Status code label");

            RecordedMeasurement duration = Find(mine, MailgunTelemetry.MetricOperationDuration);
            TestAssert.Equal(outcome, duration.Tag(MailgunTelemetry.AttributeOutcome), "Duration outcome label");
        }

        private static void AssertNoSensitiveData(TelemetryCapture capture, string[] forbidden)
        {
            foreach (Activity span in capture.Spans)
            {
                foreach (KeyValuePair<string, object?> tag in span.TagObjects)
                {
                    string val = Convert.ToString(tag.Value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
                    foreach (string f in forbidden)
                        TestAssert.False(val.Contains(f), "Span '" + span.DisplayName + "' tag '" + tag.Key + "' contains sensitive value");
                }
            }

            foreach (RecordedMeasurement m in capture.Measurements)
            {
                foreach (KeyValuePair<string, object?> tag in m.Tags)
                {
                    string val = Convert.ToString(tag.Value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
                    foreach (string f in forbidden)
                        TestAssert.False(val.Contains(f), "Metric '" + m.Instrument + "' tag '" + tag.Key + "' contains sensitive value");
                }
            }
        }

        #endregion
    }
}
