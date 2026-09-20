using DapperMany.Samples.Data;
using DapperMany.Samples.Infrastructure.Error;
using DapperMany.Samples.Infrastructure.Output;

namespace DapperMany.Samples.Services.Demos
{
    /// <summary>
    /// Defines a contract for demonstration operations that showcase DapperMany functionality.
    /// </summary>
    public interface IDemoOperation
    {
        /// <summary>
        /// Gets the name of this demo operation.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Executes the demo operation asynchronously.
        /// </summary>
        /// <param name="connection">The database connection to use for the operation.</param>
        /// <param name="output">The output formatter for displaying results.</param>
        /// <param name="errorHandler">The error handler for managing exceptions.</param>
        /// <param name="generator">The data generator for creating sample entities.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ExecuteAsync(System.Data.IDbConnection connection, IOutputFormatter output, IErrorHandler errorHandler, IPedidoGenerator generator);
    }
}
