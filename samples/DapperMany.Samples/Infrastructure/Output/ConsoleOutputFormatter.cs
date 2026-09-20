using System;

namespace DapperMany.Samples.Infrastructure.Output
{
    /// <summary>
    /// Implements console-based output formatting with colored text support.
    /// Provides safe console writing with fallback for environments where console colors are unavailable.
    /// </summary>
    public class ConsoleOutputFormatter : IOutputFormatter
    {
        private static readonly object _sync = new();

        /// <summary>
        /// Safely writes text to the console with the specified color, falling back to plain text if color is unavailable.
        /// </summary>
        /// <param name="color">The console color to apply.</param>
        /// <param name="text">The text to write.</param>
        private void SafeWrite(ConsoleColor color, string text)
        {
            // Try to set color and write; if console not available, fall back to plain write
            lock (_sync)
            {
                try
                {
                    var prev = Console.ForegroundColor;
                    Console.ForegroundColor = color;
                    Console.WriteLine(text);
                    Console.ForegroundColor = prev;
                }
                catch
                {
                    try { Console.WriteLine(text); } catch { /* swallow */ }
                }
            }
        }

        /// <summary>
        /// Writes a header message in cyan color.
        /// </summary>
        /// <param name="text">The header text to write.</param>
        public void WriteHeader(string text) => SafeWrite(ConsoleColor.Cyan, text);
        
        /// <summary>
        /// Writes an informational message in yellow color.
        /// </summary>
        /// <param name="text">The informational text to write.</param>
        public void WriteInfo(string text) => SafeWrite(ConsoleColor.Yellow, text);
        
        /// <summary>
        /// Writes a success message in green color.
        /// </summary>
        /// <param name="text">The success text to write.</param>
        public void WriteSuccess(string text) => SafeWrite(ConsoleColor.Green, text);
        
        /// <summary>
        /// Writes an error message in red color.
        /// </summary>
        /// <param name="text">The error text to write.</param>
        public void WriteError(string text) => SafeWrite(ConsoleColor.Red, text);
        
        /// <summary>
        /// Writes a plain line of text to the console with no color formatting.
        /// </summary>
        /// <param name="text">The text to write.</param>
        public void WriteLine(string text)
        {
            try { Console.WriteLine(text); } catch { /* swallow */ }
        }
    }
}
