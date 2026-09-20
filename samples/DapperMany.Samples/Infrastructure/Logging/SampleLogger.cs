using DapperMany.Samples.Infrastructure.Output;

namespace DapperMany.Samples.Infrastructure.Logging
{
    /// <summary>
    /// Provides logging functionality with formatted console output.
    /// Wraps <see cref="IOutputFormatter"/> to offer a convenient logging interface.
    /// </summary>
    public class SampleLogger
    {
        private readonly IOutputFormatter _formatter;

        /// <summary>
        /// Initializes a new instance of the <see cref="SampleLogger"/> class with a default <see cref="ConsoleOutputFormatter"/>.
        /// </summary>
        public SampleLogger() : this(new ConsoleOutputFormatter()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SampleLogger"/> class with a custom output formatter.
        /// </summary>
        /// <param name="formatter">The output formatter to use for writing log messages. Must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is null.</exception>
        public SampleLogger(IOutputFormatter formatter)
        {
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        }

        /// <summary>
        /// Gets the underlying <see cref="IOutputFormatter"/> instance used by this logger.
        /// </summary>
        public IOutputFormatter Formatter => _formatter;

        /// <summary>
        /// Writes a header message to the output formatter.
        /// </summary>
        /// <param name="text">The header text to write.</param>
        public void Header(string text) => _formatter.WriteHeader(text);

        /// <summary>
        /// Writes an informational message to the output formatter.
        /// </summary>
        /// <param name="text">The informational text to write.</param>
        public void Info(string text) => _formatter.WriteInfo(text);

        /// <summary>
        /// Writes a success message to the output formatter.
        /// </summary>
        /// <param name="text">The success text to write.</param>
        public void Success(string text) => _formatter.WriteSuccess(text);

        /// <summary>
        /// Writes an error message to the output formatter.
        /// </summary>
        /// <param name="text">The error text to write.</param>
        public void Error(string text) => _formatter.WriteError(text);

        /// <summary>
        /// Writes a line of text to the output formatter.
        /// </summary>
        /// <param name="text">The text to write.</param>
        public void Write(string text) => _formatter.WriteLine(text);

        /// <summary>
        /// Displays a prompt to the user and reads their inline input without requiring an additional line break.
        /// </summary>
        /// <param name="prompt">The prompt text to display. If null or empty, no prompt is shown.</param>
        /// <returns>The user's input, or an empty string if no input is provided.</returns>
        public string PromptInline(string prompt)
        {
            if (!string.IsNullOrEmpty(prompt))
                Console.Write(prompt);

            return Console.ReadLine() ?? string.Empty;
        }
    }
}
