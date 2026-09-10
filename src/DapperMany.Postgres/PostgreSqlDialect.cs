using DapperMany.Internal.Abstractions;

namespace DapperMany.Postgres;

/// <summary>
/// PostgreSQL-specific SQL dialect implementation.
/// Handles SQL generation for PostgreSQL 10+ databases.
/// </summary>
internal sealed class PostgreSqlDialect : ISqlDialect
{
    public string ProviderName => "PostgreSQL";

    public string GetParameterPlaceholder(int parameterIndex) => $"${parameterIndex + 1}";

    public string GetIdentityRetrievalSql() => "SELECT LASTVAL();";

    public string GetInsertSql(string tableName, IReadOnlyList<string> columnNames, int rowCount)
    {
        var quotedTable = QuoteIdentifier(tableName);
        var quotedColumns = columnNames.Select(QuoteIdentifier).ToList();

        var columnList = string.Join(", ", quotedColumns);
        var valuesList = new List<string>();

        var paramIndex = 0;
        for (int row = 0; row < rowCount; row++)
        {
            var rowValues = new List<string>();
            for (int col = 0; col < columnNames.Count; col++)
            {
                rowValues.Add(GetParameterPlaceholder(paramIndex++));
            }
            valuesList.Add($"({string.Join(", ", rowValues)})");
        }

        return $"INSERT INTO {quotedTable} ({columnList}) VALUES {string.Join(", ", valuesList)};";
    }

    public string GetUpdateSql(string tableName, IReadOnlyList<string> columnNames, string whereCondition)
    {
        var quotedTable = QuoteIdentifier(tableName);
        var setClauses = new List<string>();

        var paramIndex = 0;
        foreach (var columnName in columnNames)
        {
            var quotedColumn = QuoteIdentifier(columnName);
            var paramPlaceholder = GetParameterPlaceholder(paramIndex++);
            setClauses.Add($"{quotedColumn} = {paramPlaceholder}");
        }

        return $"""
            UPDATE {quotedTable}
            SET {string.Join(", ", setClauses)}
            {whereCondition};
            """;
    }

    public string GetDeleteSql(string tableName, string whereCondition)
    {
        var quotedTable = QuoteIdentifier(tableName);
        return $"DELETE FROM {quotedTable} {whereCondition};";
    }

    public string QuoteIdentifier(string identifier) => $"\"{identifier}\"";
}
