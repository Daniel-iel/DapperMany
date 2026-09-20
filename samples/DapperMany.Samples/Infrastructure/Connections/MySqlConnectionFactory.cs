using System.Data;
using MySqlConnector;

namespace DapperMany.Samples.Infrastructure.Connections
{
    /// <summary>
    /// Creates MySQL database connections using MySqlConnector.
    /// </summary>
    public class MySqlConnectionFactory : IConnectionFactory
    {
        /// <summary>
        /// Gets the provider name for MySQL connections.
        /// </summary>
        public string ProviderName => "MySQL";

        /// <summary>
        /// Creates a new MySQL database connection.
        /// </summary>
        /// <param name="connectionString">The MySQL connection string.</param>
        /// <returns>A new <see cref="MySqlConnection"/> instance.</returns>
        public IDbConnection Create(string connectionString)
        {
            return new MySqlConnection(connectionString);
        }
    }
}
