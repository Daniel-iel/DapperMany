using System.Data;

namespace DapperMany.Samples.Infrastructure.Connections
{
    /// <summary>
    /// Defines a contract for creating database connections.
    /// Each implementation provides database provider-specific connection creation.
    /// </summary>
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
        /// <param name="connectionString">The database connection string.</param>
        /// <returns>A new <see cref="IDbConnection"/> instance configured with the provided connection string.</returns>
        IDbConnection Create(string connectionString);
    }
}
