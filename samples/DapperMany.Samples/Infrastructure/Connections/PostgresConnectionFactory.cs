using System.Data;
using Npgsql;

namespace DapperMany.Samples.Infrastructure.Connections
{
    /// <summary>
    /// Creates PostgreSQL database connections using Npgsql.
    /// </summary>
    public class PostgresConnectionFactory : IConnectionFactory
    {
        /// <summary>
        /// Gets the provider name for PostgreSQL connections.
        /// </summary>
        public string ProviderName => "PostgreSQL";

        /// <summary>
        /// Creates a new PostgreSQL database connection.
        /// </summary>
        /// <param name="connectionString">The PostgreSQL connection string.</param>
        /// <returns>A new <see cref="NpgsqlConnection"/> instance.</returns>
        public IDbConnection Create(string connectionString)
        {
            return new NpgsqlConnection(connectionString);
        }
    }
}
