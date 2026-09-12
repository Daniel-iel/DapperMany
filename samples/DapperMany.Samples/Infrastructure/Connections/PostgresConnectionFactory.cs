using System.Data;
using Npgsql;

namespace DapperMany.Samples.Infrastructure.Connections
{
    public class PostgresConnectionFactory : IConnectionFactory
    {
        public string ProviderName => "PostgreSQL";

        public IDbConnection Create(string connectionString)
        {
            return new NpgsqlConnection(connectionString);
        }
    }
}
