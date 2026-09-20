using DapperMany.Samples.Infrastructure.Output;

namespace DapperMany.Samples.Infrastructure.Error
{
    /// <summary>
    /// Implements error handling by writing formatted error messages to the console.
    /// </summary>
    public class ConsoleErrorHandler : IErrorHandler
    {
        private readonly IOutputFormatter _output;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleErrorHandler"/> class.
        /// </summary>
        /// <param name="output">The output formatter to use for writing error messages.</param>
        public ConsoleErrorHandler(IOutputFormatter output)
        {
            _output = output;
        }

        /// <summary>
        /// Handles an exception by writing a formatted error message to the console output.
        /// </summary>
        /// <param name="ex">The exception to handle.</param>
        /// <param name="contextMessage">Optional context message that is included in the error output.</param>
        public void Handle(Exception ex, string? contextMessage = null)
        {
            if (!string.IsNullOrEmpty(contextMessage))
            {
                _output.WriteError($"  ✗ {contextMessage}: {ex.Message}");
            }
            else
            {
                _output.WriteError($"  ✗ Error: {ex.Message}");
            }
        }
    }
}
