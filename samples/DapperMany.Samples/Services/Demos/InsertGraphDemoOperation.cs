using DapperMany.Samples.Data;
using DapperMany.Samples.Infrastructure.Error;
using DapperMany.Samples.Infrastructure.Output;
using DapperMany.Samples.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DapperMany.Samples.Services.Demos
{
    public class InsertGraphDemoOperation : IDemoOperation
    {
        public string Name => "InsertManyGraph";

        public async Task ExecuteAsync(System.Data.IDbConnection connection, IOutputFormatter output, IErrorHandler errorHandler, IPedidoGenerator generator)
        {
            output.WriteInfo("\n    ⏳ Inserting orders WITH items (graph insert)...\n");

            try
            {
                var orders = new List<Pedido>
                {
                    new Pedido
                    {
                        NumeroDocumento = $"PED-GRAPH-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                        DataPedido = DateTime.UtcNow,
                        ValorTotal = 1200.00m,
                        Status = "Pendente",
                        Itens = new()
                        {
                            new ItemPedido { Descricao = "Laptop 15\"", Quantidade = 1, ValorUnitario = 800.00m },
                            new ItemPedido { Descricao = "Mouse Wireless", Quantidade = 2, ValorUnitario = 50.00m },
                            new ItemPedido { Descricao = "USB-C Cable", Quantidade = 3, ValorUnitario = 15.00m }
                        }
                    },
                    new Pedido
                    {
                        NumeroDocumento = $"PED-GRAPH-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                        DataPedido = DateTime.UtcNow,
                        ValorTotal = 500.00m,
                        Status = "Processado",
                        Itens = new()
                        {
                            new ItemPedido { Descricao = "Mechanical Keyboard", Quantidade = 1, ValorUnitario = 500.00m }
                        }
                    },
                    new Pedido
                    {
                        NumeroDocumento = $"PED-GRAPH-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                        DataPedido = DateTime.UtcNow,
                        ValorTotal = 0.00m,
                        Status = "Cancelado",
                        Itens = new() // Empty - no items
                    }
                };

                int insertedCount = await connection.InsertManyGraphAsync(orders);

                output.WriteSuccess($"    ✓ Successfully inserted {insertedCount} order(s) with items\n");
                output.WriteLine("    Details:");
                foreach (var order in orders)
                {
                    output.WriteLine($"    - Order: {order.NumeroDocumento}");
                    output.WriteLine($"      Items: {order.Itens.Count}");
                    foreach (var item in order.Itens)
                    {
                        output.WriteLine($"        • {item.Descricao} (qty: {item.Quantidade}, unit price: ${item.ValorUnitario})");
                    }
                }
                output.WriteLine(string.Empty);
            }
            catch (Exception ex)
            {
                errorHandler.Handle(ex, "Graph insert failed");
            }
        }
    }
}
