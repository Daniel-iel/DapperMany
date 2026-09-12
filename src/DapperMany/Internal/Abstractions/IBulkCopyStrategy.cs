using DapperMany.Internal.Mapping;
using System.Data;

namespace DapperMany.Internal.Abstractions;

/// <summary>
/// Strategy for bulk copying data into a database table.
/// Each provider implements this differently:
/// - SQL Server: SqlBulkCopy
/// - PostgreSQL: COPY command
/// - MySQL: LOAD DATA INFILE or multi-row INSERT
/// </summary>
public interface IBulkCopyStrategy
{
    /// <summary>
    /// Bulk inserts entities into the database table.
    /// </summary>
    /// <typeparam name="T">Entity type to insert</typeparam>
    /// <param name="connection">Active database connection</param>
    /// <param name="entities">Entities to insert</param>
    /// <param name="metadata">Entity metadata containing table and column mappings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows inserted</returns>
    Task<int> BulkInsertAsync<T>(
        IDbConnection connection,
        IEnumerable<T> entities,
        EntityMetadata metadata,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Bulk updates entities in the database table.
    /// </summary>
    /// <typeparam name="T">Entity type to update</typeparam>
    /// <param name="connection">Active database connection</param>
    /// <param name="entities">Entities to update</param>
    /// <param name="metadata">Entity metadata containing table and column mappings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows updated</returns>
    Task<int> BulkUpdateAsync<T>(
        IDbConnection connection,
        IEnumerable<T> entities,
        EntityMetadata metadata,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Bulk deletes entities from the database table.
    /// </summary>
    /// <typeparam name="T">Entity type to delete</typeparam>
    /// <param name="connection">Active database connection</param>
    /// <param name="entities">Entities to delete (only key properties used)</param>
    /// <param name="metadata">Entity metadata containing table and key mappings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows deleted</returns>
    Task<int> BulkDeleteAsync<T>(
        IDbConnection connection,
        IEnumerable<T> entities,
        EntityMetadata metadata,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Bulk deletes entities by key values only (no entity objects needed).
    /// </summary>
    /// <typeparam name="T">Entity type to delete</typeparam>
    /// <param name="connection">Active database connection</param>
    /// <param name="keys">Key values to delete</param>
    /// <param name="metadata">Entity metadata containing table and key mappings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows deleted</returns>
    Task<int> BulkDeleteByKeysAsync<T>(
        IDbConnection connection,
        IEnumerable<object> keys,
        EntityMetadata metadata,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default) where T : class;
}
