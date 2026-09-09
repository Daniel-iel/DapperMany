using System.Data;

namespace DapperMany.Internal.Abstractions;

/// <summary>
/// Strategy for retrieving generated identity values after an insert operation.
/// Different databases handle this differently:
/// - SQL Server: SCOPE_IDENTITY()
/// - PostgreSQL: RETURNING clause or lastval()
/// - MySQL: LAST_INSERT_ID()
/// </summary>
public interface IIdentityRetrievalStrategy
{
    /// <summary>
    /// Retrieves the generated identity value for a single inserted row.
    /// </summary>
    /// <param name="connection">Active database connection</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The generated identity value (typically an int or long)</returns>
    Task<object?> GetLastIdentityAsync(IDbConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves multiple generated identity values after a bulk insert operation.
    /// </summary>
    /// <param name="connection">Active database connection</param>
    /// <param name="rowCount">Number of rows that were inserted</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of generated identity values in insertion order</returns>
    Task<IList<object>> GetIdentitiesAsync(
        IDbConnection connection,
        int rowCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a SQL SELECT statement to retrieve the last identity value.
    /// Called by the bulk insert operation after rows are inserted.
    /// </summary>
    string GetIdentityRetrievalSql();
}
