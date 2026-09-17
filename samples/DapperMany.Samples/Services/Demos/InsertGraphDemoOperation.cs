using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using DapperMany.Samples.Data;
using DapperMany.Samples.Infrastructure.Error;
using DapperMany.Samples.Infrastructure.Output;
using DapperMany.Samples.Models;

namespace DapperMany.Samples.Services.Demos
{
    public class InsertGraphDemoOperation : IDemoOperation
    {
        public string Name => "InsertManyGraph";

        private readonly int _count;

        public InsertGraphDemoOperation(int count = 3)
        {
            _count = Math.Max(1, count);
        }

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
                var result = await connection.InsertManyGraphAsync(orders);
                sw.Stop();

                output.WriteSuccess($"Successfully inserted {result.TotalRowsAffected} rows in {result.Duration.TotalMilliseconds:N0} ms");
                output.WriteInfo($"  Parent orders: {result.RowsInserted}");
                if (result.RelatedEntities.TryGetValue("ItemPedido", out var itemCount))
                    output.WriteInfo($"  Child items: {itemCount}");
                
                if (result.GeneratedIds.Count > 0)
                    output.WriteInfo($"  Generated IDs: {string.Join(", ", result.GeneratedIds.Take(3))}...");
            }
            catch (Exception ex)
            {
                errorHandler.Handle(ex, "Graph insert failed");
            }
        }
    }
}
