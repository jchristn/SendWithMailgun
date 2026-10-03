namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::Xunit;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.XunitAdapter;

    /// <summary>
    /// Runs every shared SendWithMailgun descriptor sequentially in a single xUnit fact.
    /// </summary>
    public sealed class SendWithMailgunFactTests : TouchstoneFactBase
    {
        /// <summary>
        /// Shared suites under test.
        /// </summary>
        protected override IReadOnlyList<TestSuiteDescriptor> Suites
        {
            get { return SendWithMailgunSuites.All; }
        }

        /// <summary>
        /// Run all shared suites.
        /// </summary>
        /// <returns>Task.</returns>
        [Fact]
        public async Task RunAll()
        {
            await RunAllAsync();
        }
    }
}
