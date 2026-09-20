namespace DapperMany.Samples.Infrastructure.Error
{
    /// <summary>
    /// Defines a contract for handling exceptions with optional contextual information.
    /// </summary>
    public interface IErrorHandler
    {
        /// <summary>
        /// Handles an exception with optional contextual information.
        /// </summary>
        /// <param name="ex">The exception to handle.</param>
        /// <param name="contextMessage">Optional context message that provides additional information about where/why the error occurred.</param>
        void Handle(Exception ex, string? contextMessage = null);
    }
}
