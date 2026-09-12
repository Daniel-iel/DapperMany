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
    public class DeleteManyDemoOperation : IDemoOperation
    {
        public string Name => "DeleteMany";

        public async Task ExecuteAsync(System.Data.IDbConnection connection, IOutputFormatter output, IErrorHandler errorHandler, IPedidoGenerator generator)
        {
            output.WriteInfo("\n    ⏳ Deleting orders...\n");

            try
            {
                var ordersToDelete = connection.Query<Pedido>("SELECT TOP 2 * FROM Pedidos ORDER BY Id ASC").ToList();

                if (ordersToDelete.Count == 0)
                {
                    output.WriteInfo("    ℹ  No orders found to delete.\n");
                    return;
                }

                var deleteCount = await connection.DeleteManyAsync(ordersToDelete);

                output.WriteSuccess($"    ✓ Deleted {deleteCount} orders successfully");
                output.WriteLine("\n    Deleted orders:");
                foreach (var order in ordersToDelete)
                {
                    output.WriteLine($"      • Order {order.NumeroDocumento}");
                }
                output.WriteLine(string.Empty);
            }
            catch (Exception ex)
            {
                errorHandler.Handle(ex, "Delete failed");
            }
        }
    }
}
