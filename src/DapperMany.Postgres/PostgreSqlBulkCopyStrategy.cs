using Dapper;
using DapperMany.Internal.Abstractions;
using DapperMany.Internal.Mapping;
using System.Data;
using System.Diagnostics;

namespace DapperMany.Postgres;

/// <summary>
/// PostgreSQL bulk copy strategy using optimized INSERT statements.
/// Takes advantage of PostgreSQL's multi-row INSERT support and RETURNING clause.
/// </summary>
internal class PostgreSqlBulkCopyStrategy : IBulkCopyStrategy
{
    private const int MaxParametersPerBatch = 32767; // PostgreSQL parameter limit (theoretical)
    private const int RowsPerBatch = 50; // Conservative batch size

    public async Task<int> BulkInsertAsync<T>(
        IDbConnection connection,
        IEnumerable<T> entities,
        EntityMetadata metadata,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        if (metadata == null) throw new ArgumentNullException(nameof(metadata));

        var entityList = entities.ToList();
        if (entityList.Count == 0)
            return 0;

        var dialect = new PostgreSqlDialect();
        var totalInserted = 0;

        // Process in batches to avoid extremely large statements
        for (int i = 0; i < entityList.Count; i += RowsPerBatch)
        {
            var batch = entityList.Skip(i).Take(RowsPerBatch).ToList();
            var inserted = await InsertBatchAsync(connection, batch, metadata, dialect, transaction, cancellationToken);
            totalInserted += inserted;
        }

        return totalInserted;
    }

    public async Task<int> BulkUpdateAsync<T>(
        IDbConnection connection,
        IEnumerable<T> entities,
        EntityMetadata metadata,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        if (metadata == null) throw new ArgumentNullException(nameof(metadata));

        var entityList = entities.ToList();
        if (entityList.Count == 0)
            return 0;

        var dialect = new PostgreSqlDialect();
        var totalUpdated = 0;

        // Process in batches
        for (int i = 0; i < entityList.Count; i += RowsPerBatch)
        {
            var batch = entityList.Skip(i).Take(RowsPerBatch).ToList();
            var updated = await UpdateBatchAsync(connection, batch, metadata, dialect, transaction, cancellationToken);
            totalUpdated += updated;
        }

        return totalUpdated;
    }

    public async Task<int> BulkDeleteAsync<T>(
        IDbConnection connection,
        IEnumerable<T> entities,
        EntityMetadata metadata,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        if (metadata == null) throw new ArgumentNullException(nameof(metadata));

        var entityList = entities.ToList();
        if (entityList.Count == 0)
            return 0;

        var dialect = new PostgreSqlDialect();
        var totalDeleted = 0;

        // Process in batches
        for (int i = 0; i < entityList.Count; i += RowsPerBatch)
        {
            var batch = entityList.Skip(i).Take(RowsPerBatch).ToList();
            var deleted = await DeleteBatchAsync(connection, batch, metadata, dialect, transaction, cancellationToken);
            totalDeleted += deleted;
        }

        return totalDeleted;
    }

    public async Task<int> BulkDeleteByKeysAsync<T>(
        IDbConnection connection,
        IEnumerable<object> keys,
        EntityMetadata metadata,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (keys == null) throw new ArgumentNullException(nameof(keys));
        if (metadata == null) throw new ArgumentNullException(nameof(metadata));

        var keyList = keys.ToList();
        if (keyList.Count == 0)
            return 0;

        var dialect = new PostgreSqlDialect();
        var totalDeleted = 0;

        // Process in batches
        for (int i = 0; i < keyList.Count; i += RowsPerBatch)
        {
            var batch = keyList.Skip(i).Take(RowsPerBatch).ToList();
            var deleted = await DeleteKeyBatchAsync(connection, batch, metadata, dialect, transaction, cancellationToken);
            totalDeleted += deleted;
        }

        return totalDeleted;
    }

    private async Task<int> InsertBatchAsync<T>(
        IDbConnection connection,
        List<T> batch,
        EntityMetadata metadata,
        PostgreSqlDialect dialect,
        IDbTransaction? transaction,
        CancellationToken cancellationToken) where T : class
    {
        var columnNames = metadata.MappedProperties
            .Where(p => !metadata.IdentityProperties.Contains(p))
            .Select(p => p.Name)
            .ToList();

        // Build quoted names
        var sw = Stopwatch.StartNew();
        var quotedTable = dialect.QuoteIdentifier(metadata.TableName);
        var quotedColumns = columnNames.Select(c => dialect.QuoteIdentifier(c)).ToList();
        var columnList = string.Join(", ", quotedColumns);

        var valuesList = new List<string>();
        var paramIndex = 0;
        for (int row = 0; row < batch.Count; row++)
        {
            var rowValues = new List<string>();
            for (int col = 0; col < columnNames.Count; col++)
            {
                rowValues.Add(dialect.GetParameterPlaceholder(paramIndex++));
            }
            valuesList.Add($"({string.Join(", ", rowValues)})");
        }

        var parameters = BuildInsertParameters(batch, metadata, columnNames);

        // If the key property is an identity, use RETURNING to obtain IDs and set them on entities
        if (metadata.IdentityProperties.Contains(metadata.KeyProperty))
        {
            var keyQuoted = dialect.QuoteIdentifier(metadata.KeyProperty.Name);
            var sql = $"INSERT INTO {quotedTable} ({columnList}) VALUES {string.Join(", ", valuesList)} RETURNING {keyQuoted};";

            var insertedIds = (await connection.QueryAsync<int>(new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken))).Cast<object?>().ToList();

            // Assign IDs back to the entities if possible
            var setter = AccessorFactory.CreateSetter(metadata.KeyProperty);
            for (int i = 0; i < insertedIds.Count && i < batch.Count; i++)
            {
                var idValue = insertedIds[i];
                if (idValue == null)
                    continue;

                var raw = idValue;
                var t = raw.GetType();
                if (t.Namespace == "System.Data.SqlTypes")
                {
                    var valProp = t.GetProperty("Value");
                    if (valProp != null)
                        raw = valProp.GetValue(raw)!;
                }

                var targetType = metadata.KeyProperty.PropertyType;
                try
                {
                    var converted = Convert.ChangeType(raw, targetType);
                    setter(batch[i], converted!);
                }
                catch
                {
                    if (targetType.IsAssignableFrom(raw.GetType()))
                    {
                        setter(batch[i], raw);
                    }
                    else
                    {
                        if (targetType == typeof(Guid))
                        {
                            setter(batch[i], Guid.Parse(raw.ToString()!));
                        }
                        else
                        {
                            setter(batch[i], raw);
                        }
                    }
                }
            }

            sw.Stop();
            Debug.WriteLine($"[DAPPERMANY] BulkInsert {typeof(T).Name} (Postgres): affected={insertedIds.Count}, elapsed={sw.ElapsedMilliseconds}ms");
            return insertedIds.Count;
        }

        var result = await connection.ExecuteAsync(new CommandDefinition($"INSERT INTO {quotedTable} ({columnList}) VALUES {string.Join(", ", valuesList)};", parameters, transaction: transaction, cancellationToken: cancellationToken));
        sw.Stop();
        Debug.WriteLine($"[DAPPERMANY] BulkInsert {typeof(T).Name} (Postgres): affected={result}, elapsed={sw.ElapsedMilliseconds}ms");

        return result;
    }

    private async Task<int> UpdateBatchAsync<T>(
        IDbConnection connection,
        List<T> batch,
        EntityMetadata metadata,
        PostgreSqlDialect dialect,
        IDbTransaction? transaction,
        CancellationToken cancellationToken) where T : class
    {
        var totalUpdated = 0;
        var sw = Stopwatch.StartNew();

        // Update each entity individually to handle partial objects correctly
        foreach (var entity in batch)
        {
            var columnProps = metadata.MappedProperties
                .Where(p => p != metadata.KeyProperty)
                .Where(p =>
                {
                    var getter = AccessorFactory.CreateGetter(p);
                    var value = getter(entity);
                    if (value == null) return false;
                    if (p.PropertyType.IsValueType)
                    {
                        var defaultValue = Activator.CreateInstance(p.PropertyType);
                        return !object.Equals(value, defaultValue);
                    }
                    return true; // non-null reference type
                })
                .ToList();

            var columnNames = columnProps.Select(p => p.Name).ToList();

            if (columnNames.Count == 0)
                continue; // Nothing to update if only Key is present

            // Build WHERE clause for this entity's key
            var keyProperty = metadata.KeyProperty;
            var keyGetter = AccessorFactory.CreateGetter(keyProperty);
            var keyValue = keyGetter(entity);

            var whereClause = $"WHERE {dialect.QuoteIdentifier(keyProperty.Name)} = {dialect.GetParameterPlaceholder(columnNames.Count)}";
            var sql = dialect.GetUpdateSql(metadata.TableName, columnNames, whereClause);

            var parameters = new DynamicParameters();
            var paramIndex = 0;

            // Add SET clause parameters (param1..paramN)
            foreach (var prop in columnProps)
            {
                var getter = AccessorFactory.CreateGetter(prop);
                var value = getter(entity);
                parameters.Add($"@param{++paramIndex}", value ?? DBNull.Value);
            }

            // Add WHERE clause parameter (paramN+1)
            parameters.Add($"@param{++paramIndex}", keyValue);

            var updated = await connection.ExecuteAsync(
                new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));

            totalUpdated += updated;
        }

        sw.Stop();
        Debug.WriteLine($"[DAPPERMANY] BulkUpdate {typeof(T).Name} (Postgres): affected={totalUpdated}, elapsed={sw.ElapsedMilliseconds}ms");
        return totalUpdated;
    }

    private async Task<int> DeleteBatchAsync<T>(
        IDbConnection connection,
        List<T> batch,
        EntityMetadata metadata,
        PostgreSqlDialect dialect,
        IDbTransaction? transaction,
        CancellationToken cancellationToken) where T : class
    {
        var keyValues = batch
            .Select(e => AccessorFactory.CreateGetter(metadata.KeyProperty)(e))
            .ToList();

        // Use ANY operator for batch deletion (PostgreSQL idiom)
        var placeholders = string.Join(", ", Enumerable.Range(1, keyValues.Count).Select(i => $"@param{i}"));
        var whereClause = $"WHERE {dialect.QuoteIdentifier(metadata.KeyProperty.Name)} = ANY(ARRAY[{placeholders}])";
        var sql = dialect.GetDeleteSql(metadata.TableName, whereClause);

        var parameters = new DynamicParameters();
        for (int i = 0; i < keyValues.Count; i++)
        {
            parameters.Add($"@param{i + 1}", keyValues[i]);
        }

        var sw = Stopwatch.StartNew();
        var result = await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));
        sw.Stop();
        Debug.WriteLine($"[DAPPERMANY] BulkDelete {typeof(T).Name} (Postgres): affected={result}, elapsed={sw.ElapsedMilliseconds}ms");

        return result;
    }

    private async Task<int> DeleteKeyBatchAsync(
        IDbConnection connection,
        List<object> keys,
        EntityMetadata metadata,
        PostgreSqlDialect dialect,
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        // Use ANY operator for batch deletion (PostgreSQL idiom)
        var placeholders = string.Join(", ", Enumerable.Range(1, keys.Count).Select(i => $"@param{i}"));
        var whereClause = $"WHERE {dialect.QuoteIdentifier(metadata.KeyProperty.Name)} = ANY(ARRAY[{placeholders}])";
        var sql = dialect.GetDeleteSql(metadata.TableName, whereClause);

        var parameters = new DynamicParameters();
        for (int i = 0; i < keys.Count; i++)
        {
            parameters.Add($"@param{i + 1}", keys[i]);
        }

        var sw = Stopwatch.StartNew();
        var result = await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));
        sw.Stop();
        Debug.WriteLine($"[DAPPERMANY] BulkDeleteByKeys (Postgres): affected={result}, elapsed={sw.ElapsedMilliseconds}ms");

        return result;
    }

    private DynamicParameters BuildInsertParameters<T>(
        List<T> batch,
        EntityMetadata metadata,
        List<string> columnNames) where T : class
    {
        var parameters = new DynamicParameters();
        var paramIndex = 0;

        foreach (var entity in batch)
        {
            foreach (var columnName in columnNames)
            {
                var prop = metadata.MappedProperties.First(p => p.Name == columnName);
                var getter = AccessorFactory.CreateGetter(prop);
                var value = getter(entity);
                parameters.Add($"@param{++paramIndex}", value ?? DBNull.Value);
            }
        }

        return parameters;
    }
}
