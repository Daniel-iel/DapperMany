using System.Data;
using Microsoft.Data.SqlClient;

namespace DapperMany.Samples.Infrastructure.Connections
{
    /// <summary>
    /// Creates SQL Server database connections using Microsoft.Data.SqlClient.
    /// </summary>
    public class SqlServerConnectionFactory : IConnectionFactory
    {
        /// <summary>
        /// Gets the provider name for SQL Server connections.
        /// </summary>
        public string ProviderName => "SqlServer";

        /// <summary>
        /// Creates a new SQL Server database connection.
        /// </summary>
        /// <param name="connectionString">The SQL Server connection string.</param>
        /// <returns>A new <see cref="SqlConnection"/> instance.</returns>
        public IDbConnection Create(string connectionString)
        {
            return new SqlConnection(connectionString);
        }
    }
}
