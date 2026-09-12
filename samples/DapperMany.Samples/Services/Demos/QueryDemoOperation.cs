using Dapper;
using DapperMany.Samples.Data;
using DapperMany.Samples.Infrastructure.Error;
using DapperMany.Samples.Infrastructure.Output;
using DapperMany.Samples.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DapperMany.Samples.Services.Demos
{
    public class QueryDemoOperation : IDemoOperation
    {
        public string Name => "Query";

        public async Task ExecuteAsync(System.Data.IDbConnection connection, IOutputFormatter output, IErrorHandler errorHandler, IPedidoGenerator generator)
        {
            output.WriteInfo("\n    ⏳ Querying orders...\n");

            try
            {
                var orders = await connection.QueryAsync<Pedido>("SELECT Top 10 * FROM Pedidos ORDER BY Created DESC;");
                var orderList = orders.ToList();

                if (orderList.Count == 0)
                {
                    output.WriteLine("    (No orders found)");
                }
                else
                {
                    output.WriteInfo($"    Found {orderList.Count} order(s):\n");
                    foreach (var order in orderList)
                    {
                        output.WriteLine($"    - [{order.Id}] {order.NumeroDocumento}");
                        output.WriteLine($"      Status: {order.Status}, Total: ${order.ValorTotal}");
                    }
                }
            }
            catch (Exception ex)
            {
                errorHandler.Handle(ex, "Query failed");
            }
        }
    }
}
