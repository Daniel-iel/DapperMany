using DapperMany.Internal.Abstractions;

namespace DapperMany.Postgres;

/// <summary>
/// Module initializer for PostgreSQL provider.
/// Auto-registers PostgreSQL strategy, dialect, and identity retrieval when the assembly is loaded.
/// </summary>
internal static class PostgreSqlModuleInitializer
{
    #pragma warning disable CA2255 // ModuleInitializer is used intentionally for automatic provider registration.
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void Initialize()
    {
        ProviderRegistry.RegisterProvider(
            "PostgreSQL",
            new PostgreSqlDialect(),
            new PostgreSqlBulkCopyStrategy(),
            new PostgreSqlIdentityRetrievalStrategy());
    }
    #pragma warning restore CA2255
}
