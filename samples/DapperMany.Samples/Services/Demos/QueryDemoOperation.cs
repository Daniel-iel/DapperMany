using Dapper;
using DapperMany.Samples.Data;
using DapperMany.Samples.Infrastructure.Error;
using DapperMany.Samples.Infrastructure.Output;
using DapperMany.Samples.Models;
using System.Diagnostics;

namespace DapperMany.Samples.Services.Demos
{
    public class QueryDemoOperation : IDemoOperation
    {
        public string Name => "Query";

        public async Task ExecuteAsync(System.Data.IDbConnection connection, IOutputFormatter output, IErrorHandler errorHandler, IPedidoGenerator generator)
        {
            output.WriteInfo("Querying orders...");

            try
            {
                var sw = Stopwatch.StartNew();
                var orders = await connection.QueryAsync<Pedido>("SELECT Top 10 * FROM Pedidos ORDER BY Created DESC;");
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
    }
}
