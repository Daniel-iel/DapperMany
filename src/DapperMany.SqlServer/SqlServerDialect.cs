using DapperMany.Internal.Abstractions;

namespace DapperMany.SqlServer;

/// <summary>
/// SQL Server-specific SQL dialect implementation.
/// Handles SQL generation for SQL Server 2012+ databases.
/// </summary>
internal class SqlServerDialect : ISqlDialect
{
    public string ProviderName => "SqlServer";

    public string GetParameterPlaceholder(int parameterIndex) => $"@param{parameterIndex}";

    public string GetIdentityRetrievalSql() => "SELECT SCOPE_IDENTITY();";

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

    public string QuoteIdentifier(string identifier) => $"[{identifier}]";
}
