namespace Test.Shared
{
    using System;

    /// <summary>
    /// Exception thrown when a shared test assertion fails.
    /// </summary>
    public class TestAssertionException : Exception
    {
        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="message">Failure message.</param>
        public TestAssertionException(string message) : base(message)
        {
        }
    }
}
