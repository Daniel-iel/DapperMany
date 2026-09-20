using DapperMany.Internal.Abstractions;

namespace DapperMany.SqlServer;

/// <summary>
/// Provides SQL Server provider registration for DapperMany.
/// Call <see cref="Register"/> during application startup to enable SQL Server support.
/// </summary>
public static class SqlServerProvider
{
    /// <summary>
    /// Registers the SQL Server provider with DapperMany, enabling SQL Server bulk operations.
    /// Must be called during application initialization before using DapperMany with SQL Server connections.
    /// </summary>
    public static void Register()
    {
        ProviderRegistry.RegisterProvider(
            "SqlServer",
            new SqlServerDialect(),
            new SqlServerBulkCopyStrategy(),
            new SqlServerIdentityRetrievalStrategy());
    }
}
