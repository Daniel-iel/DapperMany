using DapperMany.Samples.Models;
using System;
using System.Collections.Generic;

namespace DapperMany.Samples.Data
{
    public class RandomPedidoGenerator : IPedidoGenerator
    {
        private readonly Random _rnd = new();

        public Pedido Generate()
        {
            return new Pedido
            {
                NumeroDocumento = $"PED-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                DataPedido = DateTime.UtcNow,
                ValorTotal = decimal.Round((decimal)(_rnd.NextDouble() * 10000), 2),
                Status = "Pendente"
            };
        }

        public List<Pedido> GenerateBatch(int count)
        {
            var list = new List<Pedido>(count);
            for (int i = 0; i < count; i++) list.Add(Generate());
            return list;
        }
    }
}
