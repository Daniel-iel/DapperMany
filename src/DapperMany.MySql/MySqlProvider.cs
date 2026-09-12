using DapperMany.Internal.Abstractions;

namespace DapperMany.MySql;

public static class MySqlProvider
{
    public static void Register()
    {
        ProviderRegistry.RegisterProvider(
            "MySQL",
            new MySqlDialect(),
            new MySqlBulkCopyStrategy(),
            new MySqlIdentityRetrievalStrategy());
    }
}
