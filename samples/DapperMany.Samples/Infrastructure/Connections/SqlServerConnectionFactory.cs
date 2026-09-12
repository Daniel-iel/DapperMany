using System.Data;
using Microsoft.Data.SqlClient;

namespace DapperMany.Samples.Infrastructure.Connections
{
    public class SqlServerConnectionFactory : IConnectionFactory
    {
        public string ProviderName => "SqlServer";

        public IDbConnection Create(string connectionString)
        {
            return new SqlConnection(connectionString);
        }
    }
}
