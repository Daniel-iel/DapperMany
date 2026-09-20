using Dapper;
using DapperMany.Samples.Data;
using DapperMany.Samples.Infrastructure.Error;
using DapperMany.Samples.Infrastructure.Output;
using DapperMany.Samples.Models;
using System.Diagnostics;

namespace DapperMany.Samples.Services.Demos
{
    /// <summary>
    /// Demonstrates basic querying of orders from the database.
    /// Showcases how to retrieve data and measure query performance.
    /// </summary>
    public class QueryDemoOperation : IDemoOperation
    {
        /// <summary>
        /// Gets the name of this demo operation.
        /// </summary>
        public string Name => "Query";

        /// <summary>
        /// Executes the query demo operation asynchronously.
        /// Retrieves a limited number of orders from the database and displays the results.
        /// </summary>
        /// <param name="connection">The database connection to use.</param>
        /// <param name="output">The output formatter for displaying progress and results.</param>
        /// <param name="errorHandler">The error handler for managing exceptions.</param>
        /// <param name="generator">The data generator for creating sample orders (not used in this operation).</param>
        /// <returns>A task representing the asynchronous query operation.</returns>
        public async Task ExecuteAsync(System.Data.IDbConnection connection, IOutputFormatter output, IErrorHandler errorHandler, IPedidoGenerator generator)
        {
            output.WriteInfo("Querying orders...");

            try
            {
                var sw = Stopwatch.StartNew();
                var query = GetProviderSpecificQuery(connection);
                var orders = await connection.QueryAsync<Pedido>(query);
                sw.Stop();

                var orderList = orders.ToList();

                if (orderList.Count == 0)
                {
                    output.WriteLine("(No orders found)");
                }
                else
                {
                    output.WriteSuccess($"Found {orderList.Count} order(s) in {sw.Elapsed.TotalMilliseconds:N0} ms");
                }
            }
            catch (Exception ex)
            {
                errorHandler.Handle(ex, "Query failed");
            }
        }

        /// <summary>
        /// Returns the appropriate SQL query for the current database provider.
        /// Each provider has different syntax for limiting rows:
        /// - SQL Server: SELECT Top 10
        /// - PostgreSQL: LIMIT 10
        /// - MySQL: LIMIT 10
        /// </summary>
        private static string GetProviderSpecificQuery(System.Data.IDbConnection connection)
        {
            var connectionType = connection.GetType();
            var namespaceName = connectionType.Namespace ?? "";

            return namespaceName switch
            {
                "Microsoft.Data.SqlClient" => "SELECT TOP 10 * FROM Pedidos ORDER BY Created DESC;",
                "Npgsql" => "SELECT * FROM \"Pedidos\" ORDER BY \"Created\" DESC LIMIT 10;",
                "MySqlConnector" => "SELECT * FROM Pedidos ORDER BY Created DESC LIMIT 10;",
                _ => throw new InvalidOperationException(
                    $"Unknown database provider: {connectionType.FullName}. " +
                    $"Supported providers: SQL Server (Microsoft.Data.SqlClient), " +
                    $"PostgreSQL (Npgsql), MySQL (MySqlConnector)")
            };
        }
    }
}
