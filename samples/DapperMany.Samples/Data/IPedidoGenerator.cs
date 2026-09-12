using DapperMany.Samples.Models;
using System.Collections.Generic;

namespace DapperMany.Samples.Data
{
    public interface IPedidoGenerator
    {
        Pedido Generate();
        List<Pedido> GenerateBatch(int count);
    }
}
