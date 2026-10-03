namespace Test.Nunit
{
    using System.Collections;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.NunitAdapter;

    /// <summary>
    /// Runs each shared SendWithMailgun descriptor as a separate NUnit test case.
    /// </summary>
    [TestFixture]
    public sealed class SendWithMailgunNunitTests
    {
        private static IEnumerable TestCases()
        {
            return new TouchstoneTestCaseSource(SendWithMailgunSuites.All);
        }

        /// <summary>
        /// Run a single shared test case.
        /// </summary>
        /// <param name="testCase">Test case descriptor.</param>
        /// <returns>Task.</returns>
        [Test]
        [TestCaseSource(nameof(TestCases))]
        public async Task RunTest(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
