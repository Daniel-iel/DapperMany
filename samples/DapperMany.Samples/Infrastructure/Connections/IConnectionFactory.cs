using System.Data;

namespace DapperMany.Samples.Infrastructure.Connections
{
    public interface IConnectionFactory
    {
        /// <summary>
        /// Provider name (matching configuration keys, e.g. "SqlServer", "PostgreSQL", "MySQL").
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Create a new IDbConnection instance for the given connection string.
        /// Caller is responsible for opening/disposing the connection.
        /// </summary>
        IDbConnection Create(string connectionString);
    }
}
