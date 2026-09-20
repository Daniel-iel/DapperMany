namespace DapperMany.Samples.Infrastructure.Output
{
    /// <summary>
    /// Defines a contract for formatting and writing output messages to console or other output targets.
    /// Implementations should handle color coding and safe console writing.
    /// </summary>
    public interface IOutputFormatter
    {
        /// <summary>
        /// Writes a header message.
        /// </summary>
        /// <param name="text">The header text to write.</param>
        void WriteHeader(string text);
        
        /// <summary>
        /// Writes an informational message.
        /// </summary>
        /// <param name="text">The informational text to write.</param>
        void WriteInfo(string text);
        
        /// <summary>
        /// Writes a success message.
        /// </summary>
        /// <param name="text">The success text to write.</param>
        void WriteSuccess(string text);
        
        /// <summary>
        /// Writes an error message.
        /// </summary>
        /// <param name="text">The error text to write.</param>
        void WriteError(string text);
        
        /// <summary>
        /// Writes a line of text.
        /// </summary>
        /// <param name="text">The text to write.</param>
        void WriteLine(string text);
    }
}
