using Dapper;
using System.Diagnostics;
using DapperMany.Samples.Data;
using DapperMany.Samples.Infrastructure.Error;
using DapperMany.Samples.Infrastructure.Output;
using DapperMany.Samples.Models;

namespace DapperMany.Samples.Services.Demos
{
    /// <summary>
    /// Demonstrates the DapperMany DeleteMany bulk delete functionality.
    /// </summary>
    public class DeleteManyDemoOperation : IDemoOperation
    {
        /// <summary>
        /// Gets the name of this demo operation.
        /// </summary>
        public string Name => "DeleteMany";

        private readonly int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteManyDemoOperation"/> class.
        /// </summary>
        /// <param name="count">The number of orders to delete. Defaults to 2. Minimum value is 1.</param>
        public DeleteManyDemoOperation(int count = 2)
        {
            _count = Math.Max(1, count);
        }

        /// <summary>
        /// Executes the bulk delete demo operation asynchronously.
        /// Retrieves existing orders and deletes them using DapperMany.
        /// </summary>
        /// <param name="connection">The database connection to use.</param>
        /// <param name="output">The output formatter for displaying progress and results.</param>
        /// <param name="errorHandler">The error handler for managing exceptions.</param>
        /// <param name="generator">The data generator for creating sample orders (not used in this operation).</param>
        /// <returns>A task representing the asynchronous delete operation.</returns>
        public async Task ExecuteAsync(System.Data.IDbConnection connection, IOutputFormatter output, IErrorHandler errorHandler, IPedidoGenerator generator)
        {
            output.WriteInfo("Deleting orders...");

            try
            {
                var connectionType = connection.GetType();
                var namespaceName = connectionType.Namespace ?? "";
                var query = namespaceName == "Npgsql" 
                    ? "SELECT * FROM \"Pedidos\" ORDER BY \"Id\" ASC" 
                    : "SELECT * FROM Pedidos ORDER BY Id ASC";
                var orders = connection.Query<Pedido>(query).ToList();
                var ordersToDelete = orders.Take(_count).ToList();

                if (ordersToDelete.Count == 0)
                {
                    output.WriteInfo("No orders found to delete.\n");
                    return;
                }

                var sw = Stopwatch.StartNew();
                var deleteCount = await connection.DeleteManyAsync(ordersToDelete);
                sw.Stop();

                output.WriteSuccess($"Deleted {deleteCount} orders successfully in {sw.Elapsed.TotalMilliseconds:N0} ms");
            }
            catch (Exception ex)
            {
                errorHandler.Handle(ex, "Delete failed");
            }
        }
    }
}
