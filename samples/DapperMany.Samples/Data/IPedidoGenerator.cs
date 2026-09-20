using DapperMany.Samples.Models;

namespace DapperMany.Samples.Data
{
    /// <summary>
    /// Defines a contract for generating sample order (Pedido) data.
    /// </summary>
    public interface IPedidoGenerator
    {
        /// <summary>
        /// Generates a single order with random properties.
        /// </summary>
        /// <returns>A new <see cref="Pedido"/> instance with generated data.</returns>
        Pedido Generate();
        
        /// <summary>
        /// Generates a batch of orders with random properties.
        /// </summary>
        /// <param name="count">The number of orders to generate.</param>
        /// <returns>A list of <see cref="Pedido"/> instances with generated data.</returns>
        List<Pedido> GenerateBatch(int count);
    }
}
