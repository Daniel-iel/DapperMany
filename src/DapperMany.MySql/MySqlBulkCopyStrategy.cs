using Dapper;
using DapperMany.Internal.Abstractions;
using DapperMany.Internal.Mapping;
using System.Data;
using System.Diagnostics;

namespace DapperMany.MySql;

internal class MySqlBulkCopyStrategy : IBulkCopyStrategy
{
    private const int MaxParametersPerBatch = 65535; // conservative
    private const int RowsPerBatch = 50;

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
        if (entityList.Count == 0) return 0;

        var dialect = new MySqlDialect();
        var totalInserted = 0;

        for (int i = 0; i < entityList.Count; i += RowsPerBatch)
        {
            var batch = entityList.GetRange(i, Math.Min(RowsPerBatch, entityList.Count - i));
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
        if (entityList.Count == 0) return 0;

        var dialect = new MySqlDialect();
        var totalUpdated = 0;

        for (int i = 0; i < entityList.Count; i += RowsPerBatch)
        {
            var batch = entityList.GetRange(i, Math.Min(RowsPerBatch, entityList.Count - i));
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
        if (entityList.Count == 0) return 0;

        var dialect = new MySqlDialect();
        var totalDeleted = 0;

        for (int i = 0; i < entityList.Count; i += RowsPerBatch)
        {
            var batch = entityList.GetRange(i, Math.Min(RowsPerBatch, entityList.Count - i));
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
        if (keyList.Count == 0) return 0;

        var dialect = new MySqlDialect();
        var totalDeleted = 0;

        for (int i = 0; i < keyList.Count; i += RowsPerBatch)
        {
            var batch = keyList.GetRange(i, Math.Min(RowsPerBatch, keyList.Count - i));
            var deleted = await DeleteKeyBatchAsync(connection, batch, metadata, dialect, transaction, cancellationToken);
            totalDeleted += deleted;
        }

        return totalDeleted;
    }

    private async Task<int> InsertBatchAsync<T>(
        IDbConnection connection,
        List<T> batch,
        EntityMetadata metadata,
        MySqlDialect dialect,
        IDbTransaction? transaction,
        CancellationToken cancellationToken) where T : class
    {
        var columnNames = metadata.MappedProperties
            .Where(p => !metadata.IdentityPropertySet.Contains(p))
            .Select(p => p.Name)
            .ToList();

        var sw = Stopwatch.StartNew();
        var sql = dialect.GetInsertSql(metadata.TableName, columnNames, batch.Count);
        var parameters = BuildInsertParameters(batch, metadata, columnNames);

        // If identity key, perform insert then retrieve LAST_INSERT_ID() and assign sequential IDs to entities
        if (metadata.HasIdentityKey)
        {
            var affected = await connection.ExecuteAsync(new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));

            if (affected > 0)
            {
                // Retrieve base id for the first inserted row on this connection
                var baseId = await connection.QuerySingleAsync<long>(new CommandDefinition(dialect.GetIdentityRetrievalSql(), transaction: transaction, cancellationToken: cancellationToken));

                var setter = AccessorFactory.CreateSetter(metadata.KeyProperty);
                var targetType = metadata.KeyProperty.PropertyType;

                for (int i = 0; i < batch.Count; i++)
                {
                    var idValue = baseId + i;
                    object converted;
                    try
                    {
                        converted = Convert.ChangeType(idValue, targetType);
                    }
                    catch
                    {
                        if (targetType == typeof(Guid))
                        {
                            converted = Guid.Parse(idValue.ToString());
                        }
                        else
                        {
                            converted = idValue;
                        }
                    }

                    setter(batch[i], converted);
                }
            }

            sw.Stop();
            Debug.WriteLine($"[DAPPERMANY] BulkInsert {typeof(T).Name} (MySql): affected={affected}, elapsed={sw.ElapsedMilliseconds}ms");
            return affected;
        }

        var result = await connection.ExecuteAsync(new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));
        sw.Stop();
        Debug.WriteLine($"[DAPPERMANY] BulkInsert {typeof(T).Name} (MySql): affected={result}, elapsed={sw.ElapsedMilliseconds}ms");

        return result;
    }

    private async Task<int> UpdateBatchAsync<T>(
        IDbConnection connection,
        List<T> batch,
        EntityMetadata metadata,
        MySqlDialect dialect,
        IDbTransaction? transaction,
        CancellationToken cancellationToken) where T : class
    {
        var totalUpdated = 0;
        var sw = Stopwatch.StartNew();
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
                    return true;
                })
                .ToList();

            var columnNames = columnProps.Select(p => p.Name).ToList();

            if (columnNames.Count == 0) continue;

            var keyProperty = metadata.KeyProperty;
            var keyGetter = AccessorFactory.CreateGetter(keyProperty);
            var keyValue = keyGetter(entity);

            var whereClause = $"WHERE {dialect.QuoteIdentifier(keyProperty.Name)} = @key";
            var sql = dialect.GetUpdateSql(metadata.TableName, columnNames, whereClause);

            var parameters = new DynamicParameters();
            var paramIndex = 0;
            foreach (var prop in columnProps)
            {
                var getter = AccessorFactory.CreateGetter(prop);
                var value = getter(entity);
                parameters.Add($"@param{paramIndex++}", value ?? DBNull.Value);
            }

            parameters.Add("@key", keyValue);

            var updated = await connection.ExecuteAsync(new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));
            totalUpdated += updated;
        }

        sw.Stop();
        Debug.WriteLine($"[DAPPERMANY] BulkUpdate {typeof(T).Name} (MySql): affected={totalUpdated}, elapsed={sw.ElapsedMilliseconds}ms");
        return totalUpdated;
    }

    private async Task<int> DeleteBatchAsync<T>(
        IDbConnection connection,
        List<T> batch,
        EntityMetadata metadata,
        MySqlDialect dialect,
        IDbTransaction? transaction,
        CancellationToken cancellationToken) where T : class
    {
        var keyValues = batch.Select(e => AccessorFactory.CreateGetter(metadata.KeyProperty)(e)).ToList();
        var inClause = string.Join(", ", Enumerable.Range(0, keyValues.Count).Select(i => $"@key{i}"));
        var whereClause = $"WHERE {dialect.QuoteIdentifier(metadata.KeyProperty.Name)} IN ({inClause})";
        var sql = dialect.GetDeleteSql(metadata.TableName, whereClause);

        var parameters = new DynamicParameters();
        for (int i = 0; i < keyValues.Count; i++) parameters.Add($"@key{i}", keyValues[i]);

        var sw = Stopwatch.StartNew();
        var result = await connection.ExecuteAsync(new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));
        sw.Stop();
        Debug.WriteLine($"[DAPPERMANY] BulkDelete {typeof(T).Name} (MySql): affected={result}, elapsed={sw.ElapsedMilliseconds}ms");
        return result;
    }

    private async Task<int> DeleteKeyBatchAsync(
        IDbConnection connection,
        List<object> keys,
        EntityMetadata metadata,
        MySqlDialect dialect,
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        var inClause = string.Join(", ", Enumerable.Range(0, keys.Count).Select(i => $"@key{i}"));
        var whereClause = $"WHERE {dialect.QuoteIdentifier(metadata.KeyProperty.Name)} IN ({inClause})";
        var sql = dialect.GetDeleteSql(metadata.TableName, whereClause);

        var parameters = new DynamicParameters();
        for (int i = 0; i < keys.Count; i++) parameters.Add($"@key{i}", keys[i]);

        var sw = Stopwatch.StartNew();
        var result = await connection.ExecuteAsync(new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));
        sw.Stop();
        Debug.WriteLine($"[DAPPERMANY] BulkDeleteByKeys (MySql): affected={result}, elapsed={sw.ElapsedMilliseconds}ms");
        return result;
    }

    private DynamicParameters BuildInsertParameters<T>(List<T> batch, EntityMetadata metadata, List<string> columnNames) where T : class
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
                parameters.Add($"@param{paramIndex++}", value ?? DBNull.Value);
            }
        }

        return parameters;
    }
}
