using DapperMany.Internal.Mapping;
using System.Data;

namespace DapperMany.Internal.Abstractions;

/// <summary>
/// Defines SQL dialect-specific operations for different database providers.
/// Each provider implements this to handle database-specific SQL generation.
/// </summary>
public interface ISqlDialect
{
    /// <summary>
    /// Gets the name of the database provider (e.g., "SqlServer", "PostgreSQL", "MySQL").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Generates a parameter placeholder for the given parameter index.
    /// SQL Server: @param0, @param1
    /// PostgreSQL: $1, $2
    /// MySQL: ?
    /// </summary>
    string GetParameterPlaceholder(int parameterIndex);

    /// <summary>
    /// Gets the SQL syntax for retrieving the last inserted identity value.
    /// SQL Server: SELECT SCOPE_IDENTITY();
    /// PostgreSQL: SELECT lastval();
    /// MySQL: SELECT LAST_INSERT_ID();
    /// </summary>
    string GetIdentityRetrievalSql();

    /// <summary>
    /// Gets the SQL syntax for a CTE or subquery to insert rows from a table-valued parameter or VALUES clause.
    /// </summary>
    string GetInsertSql(string tableName, IReadOnlyList<string> columnNames, int rowCount);

    /// <summary>
    /// Gets the SQL syntax for a bulk update operation.
    /// </summary>
    string GetUpdateSql(string tableName, IReadOnlyList<string> columnNames, string joinCondition);

    /// <summary>
    /// Gets the SQL syntax for a bulk delete operation.
    /// </summary>
    string GetDeleteSql(string tableName, string whereCondition);

    /// <summary>
    /// Escape/quote identifier names for the specific database.
    /// SQL Server: [ColumnName]
    /// PostgreSQL: "column_name"
    /// MySQL: `column_name`
    /// </summary>
    string QuoteIdentifier(string identifier);
}
