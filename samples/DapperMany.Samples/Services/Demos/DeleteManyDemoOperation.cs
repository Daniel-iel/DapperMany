using Dapper;
using System.Diagnostics;
using DapperMany.Samples.Data;
using DapperMany.Samples.Infrastructure.Error;
using DapperMany.Samples.Infrastructure.Output;
using DapperMany.Samples.Models;

namespace DapperMany.Samples.Services.Demos
{
    public class DeleteManyDemoOperation : IDemoOperation
    {
        public string Name => "DeleteMany";

        private readonly int _count;

        public DeleteManyDemoOperation(int count = 2)
        {
            _count = Math.Max(1, count);
        }

        public async Task ExecuteAsync(System.Data.IDbConnection connection, IOutputFormatter output, IErrorHandler errorHandler, IPedidoGenerator generator)
        {
            output.WriteInfo("Deleting orders...");

            try
            {
                var orders = connection.Query<Pedido>("SELECT * FROM Pedidos ORDER BY Id ASC").ToList();
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
