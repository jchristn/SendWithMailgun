namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using SendWithMailgun;
    using Touchstone.Core;

    /// <summary>
    /// Shared suites covering public model types.
    /// </summary>
    public static class ModelSuites
    {
        /// <summary>
        /// <see cref="MailgunValidationResult"/> and enum behavior.
        /// </summary>
        /// <returns>Suite.</returns>
        public static TestSuiteDescriptor ValidationResultSuite()
        {
            const string s = "Model";

            return new TestSuiteDescriptor(
                suiteId: s,
                displayName: "Model types",
                cases: new List<TestCaseDescriptor>
                {
                    SuiteHelpers.Case(s, "ValidationResultDefaults", "MailgunValidationResult: defaults are safe (empty reasons, Unknown result/risk)", () =>
                    {
                        MailgunValidationResult result = new MailgunValidationResult();
                        TestAssert.Null(result.Address, "Address");
                        TestAssert.False(result.IsDisposable, "IsDisposable");
                        TestAssert.False(result.IsRoleAddress, "IsRoleAddress");
                        TestAssert.NotNull(result.Reason, "Reason");
                        TestAssert.Equal(0, result.Reason.Count, "Reason count");
                        TestAssert.Equal(ResultEnum.Unknown, result.Result, "Result");
                        TestAssert.Equal(RiskEnum.Unknown, result.Risk, "Risk");
                        TestAssert.Null(result.RootAddress, "RootAddress");
                    }),

                    SuiteHelpers.Case(s, "ValidationResultSettable", "MailgunValidationResult: all properties are settable", () =>
                    {
                        MailgunValidationResult result = new MailgunValidationResult();
                        result.Address = "a@b.com";
                        result.IsDisposable = true;
                        result.IsRoleAddress = true;
                        result.Reason = new List<string> { "r1" };
                        result.Result = ResultEnum.CatchAll;
                        result.Risk = RiskEnum.Medium;
                        result.RootAddress = "root@b.com";

                        TestAssert.Equal("a@b.com", result.Address, "Address");
                        TestAssert.True(result.IsDisposable, "IsDisposable");
                        TestAssert.True(result.IsRoleAddress, "IsRoleAddress");
                        TestAssert.Equal("r1", result.Reason[0], "Reason[0]");
                        TestAssert.Equal(ResultEnum.CatchAll, result.Result, "Result");
                        TestAssert.Equal(RiskEnum.Medium, result.Risk, "Risk");
                        TestAssert.Equal("root@b.com", result.RootAddress, "RootAddress");
                    }),

                    SuiteHelpers.Case(s, "SerializeUsesWireValues", "MailgunValidationResult: serializes result/risk using Mailgun wire values", () =>
                    {
                        MailgunValidationResult result = new MailgunValidationResult();
                        result.Address = "a@b.com";
                        result.Result = ResultEnum.DoNotSend;
                        result.Risk = RiskEnum.High;

                        string json = JsonSerializer.Serialize(result);
                        TestAssert.True(json.Contains("\"result\":\"do_not_send\""), "Serialized JSON should contain result do_not_send: " + json);
                        TestAssert.True(json.Contains("\"risk\":\"high\""), "Serialized JSON should contain risk high: " + json);
                        TestAssert.True(json.Contains("\"is_disposable_address\":false"), "Serialized JSON should use Mailgun property names: " + json);
                    }),

                    SuiteHelpers.Case(s, "RoundTripAllResults", "MailgunValidationResult: every ResultEnum value survives a serialize/deserialize round trip", () =>
                    {
                        foreach (ResultEnum value in (ResultEnum[])Enum.GetValues(typeof(ResultEnum)))
                        {
                            MailgunValidationResult original = new MailgunValidationResult();
                            original.Result = value;
                            MailgunValidationResult? copy = JsonSerializer.Deserialize<MailgunValidationResult>(JsonSerializer.Serialize(original));
                            TestAssert.NotNull(copy, "Deserialized copy");
                            TestAssert.Equal(value, copy!.Result, "Round-tripped Result");
                        }
                    }),

                    SuiteHelpers.Case(s, "RoundTripAllRisks", "MailgunValidationResult: every RiskEnum value survives a serialize/deserialize round trip", () =>
                    {
                        foreach (RiskEnum value in (RiskEnum[])Enum.GetValues(typeof(RiskEnum)))
                        {
                            MailgunValidationResult original = new MailgunValidationResult();
                            original.Risk = value;
                            MailgunValidationResult? copy = JsonSerializer.Deserialize<MailgunValidationResult>(JsonSerializer.Serialize(original));
                            TestAssert.NotNull(copy, "Deserialized copy");
                            TestAssert.Equal(value, copy!.Risk, "Round-tripped Risk");
                        }
                    }),

                    SuiteHelpers.Case(s, "CallerStringEnumConverterDoesNotBreak", "MailgunValidationResult: caller options with JsonStringEnumConverter still read snake_case values", () =>
                    {
                        JsonSerializerOptions options = new JsonSerializerOptions();
                        options.Converters.Add(new JsonStringEnumConverter());
                        MailgunValidationResult? result = JsonSerializer.Deserialize<MailgunValidationResult>(
                            "{\"result\":\"do_not_send\",\"risk\":\"medium\"}", options);
                        TestAssert.NotNull(result, "Result");
                        TestAssert.Equal(ResultEnum.DoNotSend, result!.Result, "Result");
                        TestAssert.Equal(RiskEnum.Medium, result.Risk, "Risk");
                    }),

                    SuiteHelpers.Case(s, "StandaloneEnumDeserialization", "ResultEnum/RiskEnum: deserialize directly from wire values without a containing object", () =>
                    {
                        TestAssert.Equal(ResultEnum.CatchAll, JsonSerializer.Deserialize<ResultEnum>("\"catch_all\""), "ResultEnum");
                        TestAssert.Equal(RiskEnum.Low, JsonSerializer.Deserialize<RiskEnum>("\"low\""), "RiskEnum");
                        TestAssert.Equal("\"do_not_send\"", JsonSerializer.Serialize(ResultEnum.DoNotSend), "Serialized ResultEnum");
                    }),

                    SuiteHelpers.Case(s, "NumericEnumValues", "ResultEnum: defined numeric values are accepted, undefined numbers map to Unknown", () =>
                    {
                        TestAssert.Equal(ResultEnum.Undeliverable, JsonSerializer.Deserialize<ResultEnum>("1"), "Numeric 1");
                        TestAssert.Equal(ResultEnum.Unknown, JsonSerializer.Deserialize<ResultEnum>("99"), "Numeric 99");
                    }),

                    SuiteHelpers.Case(s, "NonScalarEnumThrows", "ResultEnum: object or array token throws JsonException", () =>
                    {
                        TestAssert.Throws<JsonException>(() => JsonSerializer.Deserialize<MailgunValidationResult>("{\"result\":{\"x\":1}}"));
                        TestAssert.Throws<JsonException>(() => JsonSerializer.Deserialize<MailgunValidationResult>("{\"risk\":[\"low\"]}"));
                    }),

                    SuiteHelpers.Case(s, "ResultEnumMembers", "ResultEnum: exposes Deliverable, Undeliverable, DoNotSend, CatchAll, Unknown", () =>
                    {
                        string[] names = Enum.GetNames(typeof(ResultEnum));
                        TestAssert.Equal("Deliverable,Undeliverable,DoNotSend,CatchAll,Unknown", String.Join(",", names), "ResultEnum names");
                    }),

                    SuiteHelpers.Case(s, "RiskEnumMembers", "RiskEnum: exposes High, Medium, Low, Unknown", () =>
                    {
                        string[] names = Enum.GetNames(typeof(RiskEnum));
                        TestAssert.Equal("High,Medium,Low,Unknown", String.Join(",", names), "RiskEnum names");
                    })
                });
        }
    }
}
