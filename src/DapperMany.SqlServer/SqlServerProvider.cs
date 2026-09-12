using DapperMany.Internal.Abstractions;

namespace DapperMany.SqlServer;

public static class SqlServerProvider
{
    public static void Register()
    {
        ProviderRegistry.RegisterProvider(
            "SqlServer",
            new SqlServerDialect(),
            new SqlServerBulkCopyStrategy(),
            new SqlServerIdentityRetrievalStrategy());
    }
}
