using DapperMany.Samples.Data;
using DapperMany.Samples.Infrastructure.Error;
using DapperMany.Samples.Infrastructure.Output;
using System.Threading.Tasks;

namespace DapperMany.Samples.Services.Demos
{
    public interface IDemoOperation
    {
        string Name { get; }
        Task ExecuteAsync(System.Data.IDbConnection connection, IOutputFormatter output, IErrorHandler errorHandler, IPedidoGenerator generator);
    }
}
