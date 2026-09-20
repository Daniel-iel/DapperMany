using DapperMany.Internal.Abstractions;

namespace DapperMany.MySql;

/// <summary>
/// Provides MySQL provider registration for DapperMany.
/// Call <see cref="Register"/> during application startup to enable MySQL support.
/// </summary>
public static class MySqlProvider
{
    /// <summary>
    /// Registers the MySQL provider with DapperMany, enabling MySQL bulk operations.
    /// Must be called during application initialization before using DapperMany with MySQL connections.
    /// </summary>
    public static void Register()
    {
        ProviderRegistry.RegisterProvider(
            "MySQL",
            new MySqlDialect(),
            new MySqlBulkCopyStrategy(),
            new MySqlIdentityRetrievalStrategy());
    }
}
