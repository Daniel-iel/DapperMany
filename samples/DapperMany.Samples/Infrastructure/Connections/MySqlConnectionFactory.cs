using System.Data;
using MySqlConnector;

namespace DapperMany.Samples.Infrastructure.Connections
{
    public class MySqlConnectionFactory : IConnectionFactory
    {
        public string ProviderName => "MySQL";

        public IDbConnection Create(string connectionString)
        {
            return new MySqlConnection(connectionString);
        }
    }
}
