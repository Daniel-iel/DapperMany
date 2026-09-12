using Dapper;
using System.Diagnostics;
using DapperMany.Samples.Data;
using DapperMany.Samples.Infrastructure.Error;
using DapperMany.Samples.Infrastructure.Output;
using DapperMany.Samples.Models;

namespace DapperMany.Samples.Services.Demos
{
    public class UpdateManyDemoOperation : IDemoOperation
    {
        public string Name => "UpdateMany";

        private readonly int _count;

        public UpdateManyDemoOperation(int count = 3)
        {
            _count = Math.Max(1, count);
        }

        public async Task ExecuteAsync(System.Data.IDbConnection connection, IOutputFormatter output, IErrorHandler errorHandler, IPedidoGenerator generator)
        {
            output.WriteInfo("Updating orders...");

            try
            {
                var existingOrders = connection.Query<Pedido>("SELECT * FROM Pedidos ORDER BY Id DESC").ToList();

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
