using DapperMany.Internal.Abstractions;

namespace DapperMany.MySql;

internal static class MySqlModuleInitializer
{
    #pragma warning disable CA2255 // ModuleInitializer is used intentionally for automatic provider registration.
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void Initialize()
    {
        ProviderRegistry.RegisterProvider(
            "MySQL",
            new MySqlDialect(),
            new MySqlBulkCopyStrategy(),
            new MySqlIdentityRetrievalStrategy());
    }
    #pragma warning restore CA2255
}
