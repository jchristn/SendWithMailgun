namespace Test.Shared
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Touchstone.Core;

    /// <summary>
    /// Helpers for building test case descriptors.
    /// </summary>
    public static class SuiteHelpers
    {
        /// <summary>
        /// Create an asynchronous test case.
        /// </summary>
        /// <param name="suiteId">Suite ID.</param>
        /// <param name="caseId">Case ID.</param>
        /// <param name="displayName">Display name.</param>
        /// <param name="body">Test body.</param>
        /// <returns>Test case descriptor.</returns>
        public static TestCaseDescriptor Case(string suiteId, string caseId, string displayName, Func<CancellationToken, Task> body)
        {
            return new TestCaseDescriptor(
                suiteId: suiteId,
                caseId: caseId,
                displayName: displayName,
                executeAsync: body);
        }

        /// <summary>
        /// Create a synchronous test case.
        /// </summary>
        /// <param name="suiteId">Suite ID.</param>
        /// <param name="caseId">Case ID.</param>
        /// <param name="displayName">Display name.</param>
        /// <param name="body">Test body.</param>
        /// <returns>Test case descriptor.</returns>
        public static TestCaseDescriptor Case(string suiteId, string caseId, string displayName, Action body)
        {
            return new TestCaseDescriptor(
                suiteId: suiteId,
                caseId: caseId,
                displayName: displayName,
                executeAsync: ct =>
                {
                    ct.ThrowIfCancellationRequested();
                    body();
                    return Task.CompletedTask;
                });
        }
    }
}
