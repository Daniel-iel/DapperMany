using DapperMany.Internal.Abstractions;

namespace DapperMany.Postgres;

public static class PostgreSqlProvider
{
    public static void Register()
    {
        ProviderRegistry.RegisterProvider(
            "PostgreSQL",
            new PostgreSqlDialect(),
            new PostgreSqlBulkCopyStrategy(),
            new PostgreSqlIdentityRetrievalStrategy());
    }
}
