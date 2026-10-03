namespace Test.Shared
{
    using System.Collections.Generic;
    using Touchstone.Core;

    /// <summary>
    /// Shared Touchstone suite catalog for SendWithMailgun.  Every runner (Test.Automated, Test.Xunit, Test.Nunit) consumes <see cref="All"/>.
    /// </summary>
    public static class SendWithMailgunSuites
    {
        /// <summary>
        /// All shared test suites.
        /// </summary>
        public static IReadOnlyList<TestSuiteDescriptor> All
        {
            get
            {
                return new List<TestSuiteDescriptor>
                {
                    SenderSuites.ConstructorSuite(),
                    SenderSuites.ArgumentValidationSuite(),
                    SenderSuites.RequestSuite(),
                    SenderSuites.ResponseSuite(),
                    SenderSuites.FailureSuite(),
                    ValidatorSuites.ConstructorSuite(),
                    ValidatorSuites.ArgumentValidationSuite(),
                    ValidatorSuites.RequestSuite(),
                    ValidatorSuites.ResponseSuite(),
                    ValidatorSuites.FailureSuite(),
                    ModelSuites.ValidationResultSuite(),
                    TelemetrySuites.ContractSuite(),
                    TelemetrySuites.SenderSuite(),
                    TelemetrySuites.ValidatorSuite()
                };
            }
        }
    }
}
