using Dapper;
using System.Diagnostics;
using DapperMany.Samples.Data;
using DapperMany.Samples.Infrastructure.Error;
using DapperMany.Samples.Infrastructure.Output;
using DapperMany.Samples.Models;

namespace DapperMany.Samples.Services.Demos
{
    /// <summary>
    /// Demonstrates the DapperMany UpdateMany bulk update functionality.
    /// </summary>
    public class UpdateManyDemoOperation : IDemoOperation
    {
        /// <summary>
        /// Gets the name of this demo operation.
        /// </summary>
        public string Name => "UpdateMany";

        private readonly int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateManyDemoOperation"/> class.
        /// </summary>
        /// <param name="count">The number of orders to update. Defaults to 3. Minimum value is 1.</param>
        public UpdateManyDemoOperation(int count = 3)
        {
            _count = Math.Max(1, count);
        }

        /// <summary>
        /// Executes the bulk update demo operation asynchronously.
        /// Retrieves existing orders, modifies their status, and updates them using DapperMany.
        /// </summary>
        /// <param name="connection">The database connection to use.</param>
        /// <param name="output">The output formatter for displaying progress and results.</param>
        /// <param name="errorHandler">The error handler for managing exceptions.</param>
        /// <param name="generator">The data generator for creating sample orders (not used in this operation).</param>
        /// <returns>A task representing the asynchronous update operation.</returns>
        public async Task ExecuteAsync(System.Data.IDbConnection connection, IOutputFormatter output, IErrorHandler errorHandler, IPedidoGenerator generator)
        {
            output.WriteInfo("Updating orders...");

            try
            {
                var connectionType = connection.GetType();
                var namespaceName = connectionType.Namespace ?? "";
                var query = namespaceName == "Npgsql" 
                    ? "SELECT * FROM \"Pedidos\" ORDER BY \"Id\" DESC" 
                    : "SELECT * FROM Pedidos ORDER BY Id DESC";
                var existingOrders = connection.Query<Pedido>(query).ToList();

                var ordersToUpdate = existingOrders.Take(_count).ToList();
                if (ordersToUpdate.Count == 0)
                {
                    output.WriteInfo("No orders found to update. Insert some orders first.\n");
                    return;
                }

                foreach (var order in ordersToUpdate)
                {
                    order.Status = order.Status switch
                    {
                        "Pendente" => "Processado",
                        "Processado" => "Entregue",
                        "Entregue" => "Pendente",
                        _ => "Processado"
                    };
                    order.Modified = DateTime.UtcNow;
                }

                var sw = Stopwatch.StartNew();
                var updatedCount = await connection.UpdateManyAsync(ordersToUpdate);
                sw.Stop();

                output.WriteSuccess($"Updated {updatedCount} orders successfully in {sw.Elapsed.TotalMilliseconds:N0} ms");
            }
            catch (Exception ex)
            {
                errorHandler.Handle(ex, "Update failed");
            }
        }
    }
}
