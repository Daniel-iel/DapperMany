using System.Diagnostics;
using DapperMany.Samples.Data;
using DapperMany.Samples.Infrastructure.Error;
using DapperMany.Samples.Infrastructure.Output;
using DapperMany.Samples.Models;

namespace DapperMany.Samples.Services.Demos
{
    /// <summary>
    /// Demonstrates the DapperMany InsertMany bulk insert functionality.
    /// </summary>
    public class InsertManyDemoOperation : IDemoOperation
    {
        /// <summary>
        /// Gets the name of this demo operation.
        /// </summary>
        public string Name => "InsertMany";

        private readonly int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="InsertManyDemoOperation"/> class.
        /// </summary>
        /// <param name="count">The number of orders to insert. Defaults to 1500.</param>
        public InsertManyDemoOperation(int count = 1500)
        {
            _count = count;
        }

        /// <summary>
        /// Executes the bulk insert demo operation asynchronously.
        /// </summary>
        /// <param name="connection">The database connection to use.</param>
        /// <param name="output">The output formatter for displaying progress and results.</param>
        /// <param name="errorHandler">The error handler for managing exceptions.</param>
        /// <param name="generator">The data generator for creating sample orders.</param>
        /// <returns>A task representing the asynchronous insert operation.</returns>
        public async Task ExecuteAsync(System.Data.IDbConnection connection, IOutputFormatter output, IErrorHandler errorHandler, IPedidoGenerator generator)
        {
            output.WriteInfo($"Inserting {_count} sample orders...");

            try
            {
                List<Pedido> orders = generator.GenerateBatch(_count);

                var sw = Stopwatch.StartNew();
                var rowsInserted = await connection.InsertManyAsync(orders);
                sw.Stop();

                output.WriteSuccess($"Inserted {rowsInserted} order(s) in {sw.Elapsed.TotalMilliseconds:N0} ms");
            }
            catch (Exception ex)
            {
                errorHandler.Handle(ex, "Insert failed");
            }
        }
    }
}
