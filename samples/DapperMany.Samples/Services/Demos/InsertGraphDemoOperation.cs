using DapperMany.Samples.Data;
using DapperMany.Samples.Infrastructure.Error;
using DapperMany.Samples.Infrastructure.Output;
using DapperMany.Samples.Models;
using System.Diagnostics;

namespace DapperMany.Samples.Services.Demos
{
    /// <summary>
    /// Demonstrates the DapperMany InsertManyGraph bulk insert with object graph functionality.
    /// </summary>
    public class InsertGraphDemoOperation : IDemoOperation
    {
        /// <summary>
        /// Gets the name of this demo operation.
        /// </summary>
        public string Name => "InsertManyGraph";

        private readonly int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="InsertGraphDemoOperation"/> class.
        /// </summary>
        /// <param name="count">The number of orders with items to insert. Defaults to 3. Minimum value is 1.</param>
        public InsertGraphDemoOperation(int count = 3)
        {
            _count = Math.Max(1, count);
        }

        /// <summary>
        /// Executes the bulk insert graph demo operation asynchronously.
        /// Demonstrates inserting orders along with their related items in a single operation.
        /// </summary>
        /// <param name="connection">The database connection to use.</param>
        /// <param name="output">The output formatter for displaying progress and results.</param>
        /// <param name="errorHandler">The error handler for managing exceptions.</param>
        /// <param name="generator">The data generator for creating sample orders.</param>
        /// <returns>A task representing the asynchronous insert graph operation.</returns>
        public async Task ExecuteAsync(System.Data.IDbConnection connection, IOutputFormatter output, IErrorHandler errorHandler, IPedidoGenerator generator)
        {
            output.WriteInfo($"Inserting {_count} orders WITH items (graph insert)...");

            try
            {
                var orders = new List<Pedido>(_count);
                var rnd = new Random();

                for (int i = 0; i < _count; i++)
                {
                    var order = generator.Generate();
                    order.NumeroDocumento = $"PED-GRAPH-{Guid.NewGuid().ToString()[..8].ToUpper()}";
                    order.DataPedido = DateTime.UtcNow;

                    int itemsCount = rnd.Next(1, 4); // 1..3 items
                    for (int j = 0; j < itemsCount; j++)
                    {
                        var item = new ItemPedido
                        {
                            Descricao = $"Item-{j + 1}",
                            Quantidade = rnd.Next(1, 5),
                            ValorUnitario = decimal.Round((decimal)(rnd.NextDouble() * 500), 2)
                        };
                        item.ValorTotal = item.Quantidade * item.ValorUnitario;
                        order.Itens.Add(item);
                    }

                    order.ValorTotal = order.Itens.Sum(x => x.ValorTotal);
                    orders.Add(order);
                }

                var sw = Stopwatch.StartNew();
                int insertedCount = await connection.InsertManyAsync(orders);
                sw.Stop();

                output.WriteSuccess($"Successfully inserted {insertedCount} order(s) with items in {sw.Elapsed.TotalMilliseconds:N0} ms");
            }
            catch (Exception ex)
            {
                errorHandler.Handle(ex, "Graph insert failed");
            }
        }
    }
}
