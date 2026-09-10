using Dapper;
using DapperMany.Internal.Abstractions;
using System.Data;

namespace DapperMany.SqlServer;

/// <summary>
/// SQL Server identity retrieval strategy using SCOPE_IDENTITY() and IDENT_CURRENT().
/// </summary>
internal sealed class SqlServerIdentityRetrievalStrategy : IIdentityRetrievalStrategy
{
    public async Task<object?> GetLastIdentityAsync(IDbConnection connection, CancellationToken cancellationToken = default)
    {
        var sql = GetIdentityRetrievalSql();
        var result = await connection.QuerySingleOrDefaultAsync<object?>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return result;
    }

    public async Task<IList<object>> GetIdentitiesAsync(
        IDbConnection connection,
        int rowCount,
        CancellationToken cancellationToken = default)
    {
        if (rowCount <= 0)
            return new List<object>();

        // For SQL Server, we need to query the inserted IDs after the insert
        // This is typically done with OUTPUT clause or by querying IDENT_CURRENT
        // For now, return empty list - actual implementation handled in BulkInsertAsync
        var identities = new List<object>();
        for (int i = 0; i < rowCount; i++)
        {
            var id = await GetLastIdentityAsync(connection, cancellationToken);
            if (id is null)
                throw new InvalidOperationException("Failed to retrieve identity value from database.");

            identities.Add(id);
        }

        return identities;
    }

    public string GetIdentityRetrievalSql() => "SELECT SCOPE_IDENTITY() AS Id;";
}
