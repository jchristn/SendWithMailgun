namespace Test.Nunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.NunitAdapter;

    /// <summary>
    /// Runs every shared SendWithMailgun descriptor sequentially in a single NUnit test.
    /// </summary>
    [TestFixture]
    public sealed class SendWithMailgunNunitFactTests : TouchstoneNunitBase
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
        [Test]
        public async Task RunAll()
        {
            await RunAllAsync().ConfigureAwait(false);
        }
    }
}
