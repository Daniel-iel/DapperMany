using System.Diagnostics;
using DapperMany.Samples.Data;
using DapperMany.Samples.Infrastructure.Error;
using DapperMany.Samples.Infrastructure.Output;
using DapperMany.Samples.Models;

namespace DapperMany.Samples.Services.Demos
{
    public class InsertManyDemoOperation : IDemoOperation
    {
        public string Name => "InsertMany";

        private readonly int _count;

        public InsertManyDemoOperation(int count = 1500)
        {
            _count = count;
        }

        public async Task ExecuteAsync(System.Data.IDbConnection connection, IOutputFormatter output, IErrorHandler errorHandler, IPedidoGenerator generator)
        {
            output.WriteInfo($"Inserting {_count} sample orders...");

            try
            {
                List<Pedido> orders = generator.GenerateBatch(_count);

                var sw = Stopwatch.StartNew();
                var result = await connection.InsertManyAsync(orders);
                sw.Stop();

                output.WriteSuccess($"Inserted {result.RowsInserted} order(s) in {result.Duration.TotalMilliseconds:N0} ms");
                if (result.GeneratedIds.Count > 0)
                    output.WriteInfo($"Generated IDs: {string.Join(", ", result.GeneratedIds.Take(3))}...");
                if (!result.IsSuccessful)
                    foreach (var error in result.Errors)
                        output.WriteError($"Error at index {error.EntityIndex}: {error.Message}");
            }
            catch (Exception ex)
            {
                errorHandler.Handle(ex, "Insert failed");
            }
        }
    }
}
