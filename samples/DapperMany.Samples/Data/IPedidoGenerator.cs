using DapperMany.Samples.Models;

namespace DapperMany.Samples.Data
{
    public interface IPedidoGenerator
    {
        Pedido Generate();
        List<Pedido> GenerateBatch(int count);
    }
}
