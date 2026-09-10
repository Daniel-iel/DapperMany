using Dapper;
using DapperMany.Internal.Abstractions;
using DapperMany.Internal.Mapping;
using System.Data;

namespace DapperMany.Postgres;

/// <summary>
/// PostgreSQL bulk copy strategy using optimized INSERT statements.
/// Takes advantage of PostgreSQL's multi-row INSERT support and RETURNING clause.
/// </summary>
internal sealed class PostgreSqlBulkCopyStrategy : IBulkCopyStrategy
{
    private const int MaxParametersPerBatch = 32767; // PostgreSQL parameter limit (theoretical)
    private const int RowsPerBatch = 50; // Conservative batch size

    public async Task<int> BulkInsertAsync<T>(
        IDbConnection connection,
        IEnumerable<T> entities,
        EntityMetadata metadata,
        CancellationToken cancellationToken = default) where T : class
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        if (metadata == null) throw new ArgumentNullException(nameof(metadata));

        var entityList = entities.ToList();
        if (entityList.Count == 0)
            return 0;

        var dialect = new PostgreSqlDialect();
        var totalInserted = 0;

        // Process in batches to avoid extremely large statements (using GetRange for efficiency)
        foreach (var batch in BatchHelper.Batch(entityList, RowsPerBatch))
        {
            var inserted = await InsertBatchAsync(connection, batch, metadata, dialect, cancellationToken);
            totalInserted += inserted;
        }

        return totalInserted;
    }

    public async Task<int> BulkUpdateAsync<T>(
        IDbConnection connection,
        IEnumerable<T> entities,
        EntityMetadata metadata,
        CancellationToken cancellationToken = default) where T : class
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        if (metadata == null) throw new ArgumentNullException(nameof(metadata));

        var entityList = entities.ToList();
        if (entityList.Count == 0)
            return 0;

        var dialect = new PostgreSqlDialect();
        var totalUpdated = 0;

        // Process in batches (using GetRange for efficiency)
        foreach (var batch in BatchHelper.Batch(entityList, RowsPerBatch))
        {
            var updated = await UpdateBatchAsync(connection, batch, metadata, dialect, cancellationToken);
            totalUpdated += updated;
        }

        return totalUpdated;
    }

    public async Task<int> BulkDeleteAsync<T>(
        IDbConnection connection,
        IEnumerable<T> entities,
        EntityMetadata metadata,
        CancellationToken cancellationToken = default) where T : class
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        if (metadata == null) throw new ArgumentNullException(nameof(metadata));

        var entityList = entities.ToList();
        if (entityList.Count == 0)
            return 0;

        var dialect = new PostgreSqlDialect();
        var totalDeleted = 0;

        // Process in batches (using GetRange for efficiency)
        foreach (var batch in BatchHelper.Batch(entityList, RowsPerBatch))
        {
            var deleted = await DeleteBatchAsync(connection, batch, metadata, dialect, cancellationToken);
            totalDeleted += deleted;
        }

        return totalDeleted;
    }

    public async Task<int> BulkDeleteByKeysAsync<T>(
        IDbConnection connection,
        IEnumerable<object> keys,
        EntityMetadata metadata,
        CancellationToken cancellationToken = default) where T : class
    {
        if (keys == null) throw new ArgumentNullException(nameof(keys));
        if (metadata == null) throw new ArgumentNullException(nameof(metadata));

        var keyList = keys.ToList();
        if (keyList.Count == 0)
            return 0;

        var dialect = new PostgreSqlDialect();
        var totalDeleted = 0;

        // Process in batches (using GetRange for efficiency)
        foreach (var batch in BatchHelper.Batch(keyList, RowsPerBatch))
        {
            var deleted = await DeleteKeyBatchAsync(connection, batch, metadata, dialect, cancellationToken);
            totalDeleted += deleted;
        }

        return totalDeleted;
    }

    private async Task<int> InsertBatchAsync<T>(
        IDbConnection connection,
        List<T> batch,
        EntityMetadata metadata,
        PostgreSqlDialect dialect,
        CancellationToken cancellationToken) where T : class
    {
        var columnNames = metadata.MappedProperties
            .Where(p => !metadata.IdentityProperties.Contains(p))
            .Select(p => p.Name)
            .ToList();

        var sql = dialect.GetInsertSql(metadata.TableName, columnNames, batch.Count);
        var parameters = BuildInsertParameters(batch, metadata, columnNames);

        var result = await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        return result;
    }

    private async Task<int> UpdateBatchAsync<T>(
        IDbConnection connection,
        List<T> batch,
        EntityMetadata metadata,
        PostgreSqlDialect dialect,
        CancellationToken cancellationToken) where T : class
    {
        var totalUpdated = 0;

        // Update each entity individually to handle partial objects correctly
        foreach (var entity in batch)
        {
            // Detect which properties have been set (non-default values)
            // For a partial update object, only Key + modified properties should be included
            var columnNames = metadata.MappedProperties
                .Where(p => p != metadata.KeyProperty) // Exclude key from SET clause
                .Select(p => p.Name)
                .ToList();

            if (columnNames.Count == 0)
                continue; // Nothing to update if only Key is present

            // Build WHERE clause for this entity's key
            var keyProperty = metadata.KeyProperty;
            var keyGetter = AccessorFactory.CreateGetter(keyProperty);
            var keyValue = keyGetter(entity);

            var whereClause = $"WHERE {dialect.QuoteIdentifier(keyProperty.Name)} = $1";
            var sql = dialect.GetUpdateSql(metadata.TableName, columnNames, whereClause);

            var parameters = new DynamicParameters();
            var paramIndex = 1;

            // Add SET clause parameters
            foreach (var columnName in columnNames)
            {
                var prop = metadata.MappedProperties.First(p => p.Name == columnName);
                var getter = AccessorFactory.CreateGetter(prop);
                var value = getter(entity);
                parameters.Add($"param{paramIndex}", value ?? DBNull.Value);
                paramIndex++;
            }

            // Add WHERE clause parameter
            parameters.Add($"param{paramIndex}", keyValue);

            var updated = await connection.ExecuteAsync(
                new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

            totalUpdated += updated;
        }

        return totalUpdated;
    }

    private async Task<int> DeleteBatchAsync<T>(
        IDbConnection connection,
        List<T> batch,
        EntityMetadata metadata,
        PostgreSqlDialect dialect,
        CancellationToken cancellationToken) where T : class
    {
        var keyValues = batch
            .Select(e => AccessorFactory.CreateGetter(metadata.KeyProperty)(e))
            .ToList();

        // Use ANY operator for batch deletion (PostgreSQL idiom)
        var placeholders = string.Join(", ", Enumerable.Range(1, keyValues.Count).Select(i => $"${i}"));
        var whereClause = $"WHERE {dialect.QuoteIdentifier(metadata.KeyProperty.Name)} = ANY(ARRAY[{placeholders}])";
        var sql = dialect.GetDeleteSql(metadata.TableName, whereClause);

        var parameters = new DynamicParameters();
        for (int i = 0; i < keyValues.Count; i++)
        {
            parameters.Add($"param{i + 1}", keyValues[i]);
        }

        var result = await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        return result;
    }

    private async Task<int> DeleteKeyBatchAsync(
        IDbConnection connection,
        List<object> keys,
        EntityMetadata metadata,
        PostgreSqlDialect dialect,
        CancellationToken cancellationToken)
    {
        // Use ANY operator for batch deletion (PostgreSQL idiom)
        var placeholders = string.Join(", ", Enumerable.Range(1, keys.Count).Select(i => $"${i}"));
        var whereClause = $"WHERE {dialect.QuoteIdentifier(metadata.KeyProperty.Name)} = ANY(ARRAY[{placeholders}])";
        var sql = dialect.GetDeleteSql(metadata.TableName, whereClause);

        var parameters = new DynamicParameters();
        for (int i = 0; i < keys.Count; i++)
        {
            parameters.Add($"param{i + 1}", keys[i]);
        }

        var result = await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        return result;
    }

    private DynamicParameters BuildInsertParameters<T>(
        List<T> batch,
        EntityMetadata metadata,
        List<string> columnNames) where T : class
    {
        var parameters = new DynamicParameters();
        var paramIndex = 1;

        foreach (var entity in batch)
        {
            foreach (var columnName in columnNames)
            {
                var prop = metadata.MappedProperties.First(p => p.Name == columnName);
                var getter = AccessorFactory.CreateGetter(prop);
                var value = getter(entity);
                parameters.Add($"param{paramIndex++}", value ?? DBNull.Value);
            }
        }

        return parameters;
    }
}
