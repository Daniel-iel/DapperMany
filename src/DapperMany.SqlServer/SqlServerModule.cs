using DapperMany.Internal.Abstractions;
using System.Runtime.CompilerServices;

namespace DapperMany.SqlServer;

/// <summary>
/// SQL Server provider module initialization.
/// Automatically registers the SQL Server provider when the DapperMany.SqlServer assembly is loaded.
/// </summary>
internal static class SqlServerModule
{
    /// <summary>
    /// Module initializer that runs automatically when the assembly is loaded.
    /// Registers the SQL Server provider with the global provider registry.
    /// </summary>
    #pragma warning disable CA2255 // ModuleInitializer is used intentionally for automatic provider registration.
    [ModuleInitializer]
    public static void Initialize()
    {
        ProviderRegistry.RegisterProvider(
            "SqlServer",
            new SqlServerDialect(),
            new SqlServerBulkCopyStrategy(),
            new SqlServerIdentityRetrievalStrategy());
    }
    #pragma warning restore CA2255
}
