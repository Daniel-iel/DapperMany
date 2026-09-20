using DapperMany.Samples.Models;

namespace DapperMany.Samples.Data
{
    /// <summary>
    /// Generates random order (Pedido) data for testing and demonstration purposes.
    /// </summary>
    public class RandomPedidoGenerator : IPedidoGenerator
    {
        private readonly Random _rnd = new();

        /// <summary>
        /// Generates a single order with random document number, date, total value, and status.
        /// </summary>
        /// <returns>A new <see cref="Pedido"/> instance with randomly generated properties.</returns>
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

        /// <summary>
        /// Generates a batch of orders with random properties.
        /// </summary>
        /// <param name="count">The number of orders to generate.</param>
        /// <returns>A list of newly generated <see cref="Pedido"/> instances.</returns>
        public List<Pedido> GenerateBatch(int count)
        {
            var list = new List<Pedido>(count);
            for (int i = 0; i < count; i++) list.Add(Generate());
            return list;
        }
    }
}
