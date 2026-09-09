using Dapper;
using DapperMany.Internal.Abstractions;
using System.Data;

namespace DapperMany.Postgres;

internal class PostgreSqlIdentityRetrievalStrategy : IIdentityRetrievalStrategy
{
    public async Task<object?> GetLastIdentityAsync(IDbConnection connection, CancellationToken cancellationToken = default)
    {
        var sql = "SELECT LASTVAL() AS Id;";
        var result = await connection.QuerySingleOrDefaultAsync<object?>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return result;
    }

    public async Task<IList<object>> GetIdentitiesAsync(IDbConnection connection, int rowCount, CancellationToken cancellationToken = default)
    {
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

    public string GetIdentityRetrievalSql() => "SELECT LASTVAL() AS Id;";
}