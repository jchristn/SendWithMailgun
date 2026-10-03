namespace Test.Automated
{
    using System.Threading.Tasks;
    using Test.Shared;
    using Touchstone.Cli;

    /// <summary>
    /// Console-based Touchstone test runner for SendWithMailgun.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args">Command-line arguments.  Use --results [file] to export results as JSON.</param>
        /// <returns>0 if all tests pass, 1 if any fail.</returns>
        public static async Task<int> Main(string[] args)
        {
            string? resultsPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--results" && i + 1 < args.Length)
                {
                    resultsPath = args[i + 1];
                    break;
                }
            }

            return await ConsoleRunner.RunAsync(
                SendWithMailgunSuites.All,
                resultsPath: resultsPath).ConfigureAwait(false);
        }
    }
}
