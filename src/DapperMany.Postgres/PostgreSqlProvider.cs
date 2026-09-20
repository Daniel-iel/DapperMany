using DapperMany.Internal.Abstractions;

namespace DapperMany.Postgres;

/// <summary>
/// Provides PostgreSQL provider registration for DapperMany.
/// Call <see cref="Register"/> during application startup to enable PostgreSQL support.
/// </summary>
public static class PostgreSqlProvider
{
    /// <summary>
    /// Registers the PostgreSQL provider with DapperMany, enabling PostgreSQL bulk operations.
    /// Must be called during application initialization before using DapperMany with PostgreSQL connections.
    /// </summary>
    public static void Register()
    {
        ProviderRegistry.RegisterProvider(
            "PostgreSQL",
            new PostgreSqlDialect(),
            new PostgreSqlBulkCopyStrategy(),
            new PostgreSqlIdentityRetrievalStrategy());
    }
}
