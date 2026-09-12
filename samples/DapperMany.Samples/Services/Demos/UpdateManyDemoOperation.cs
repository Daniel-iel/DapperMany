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
    public class UpdateManyDemoOperation : IDemoOperation
    {
        public string Name => "UpdateMany";

        public async Task ExecuteAsync(System.Data.IDbConnection connection, IOutputFormatter output, IErrorHandler errorHandler, IPedidoGenerator generator)
        {
            output.WriteInfo("\n    ⏳ Updating orders...\n");

            try
            {
                var existingOrders = connection.Query<Pedido>("SELECT TOP 3 * FROM Pedidos ORDER BY Id DESC");

                var ordersToUpdate = existingOrders.ToList();
                if (ordersToUpdate.Count == 0)
                {
                    output.WriteInfo("    ℹ  No orders found to update. Insert some orders first.\n");
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

                var updatedCount = await connection.UpdateManyAsync(ordersToUpdate);

                output.WriteSuccess($"    ✓ Updated {updatedCount} orders successfully");
                output.WriteLine("\n    Updated orders:");
                foreach (var order in ordersToUpdate)
                {
                    output.WriteLine($"      • Order {order.NumeroDocumento}: {order.Status}");
                }
                output.WriteLine(string.Empty);
            }
            catch (Exception ex)
            {
                errorHandler.Handle(ex, "Update failed");
            }
        }
    }
}
