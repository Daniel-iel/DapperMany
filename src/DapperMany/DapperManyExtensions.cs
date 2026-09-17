using DapperMany.Internal.Abstractions;
using DapperMany.Internal.Mapping;
using System.Data;

namespace DapperMany;

/// <summary>
/// DapperMany extension methods for IDbConnection.
/// Provides InsertMany, UpdateMany, DeleteMany operations for bulk database operations.
/// </summary>
public static class DapperManyExtensions
{
    /// <summary>
    /// Inserts multiple entities into the database in a single batch operation.
    /// Automatically detects relationships ([HasMany], [HasOne] attributes) and handles graph insertion if present.
    /// </summary>
    /// <typeparam name="T">Entity type to insert</typeparam>
    /// <param name="connection">Database connection</param>
    /// <param name="entities">Entities to insert (may include populated child collections for graph inserts)</param>
    /// <param name="tx">Optional external transaction</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of parent entities inserted. Child entities are also inserted but not counted in the return value.</returns>
    /// <exception cref="InvalidOperationException">If the entity type has no [Table] attribute or provider is not registered</exception>
    /// <remarks>
    /// This method automatically detects whether the entity type has relationships and routes to the appropriate insert strategy:
    /// 
    /// Example 1: Flat collection (no relationships)
    /// <code>
    /// var products = new List&lt;Product&gt; { new Product { Name = "Item 1" }, ... };
    /// var count = await connection.InsertManyAsync(products);
    /// // count = number of products inserted
    /// </code>
    /// 
    /// Example 2: Graph with children (auto-detected)
    /// <code>
    /// var orders = new List&lt;Pedido&gt; {
    ///     new Pedido { NumeroDocumento = "PED-001", Itens = new List&lt;ItemPedido&gt; {
    ///         new ItemPedido { Descricao = "Item 1", Quantidade = 1, ValorUnitario = 100 }
    ///     }}
    /// };
    /// var count = await connection.InsertManyAsync(orders);
    /// // count = number of Pedidos inserted (3 in this example)
    /// // ItemPedido.PedidoId will be auto-populated from Pedido.Id
    /// </code>
    /// </remarks>
    public static Task<int> InsertManyAsync<T>(
        this IDbConnection connection,
        IEnumerable<T> entities,
        IDbTransaction? tx = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));
        
        var metadata = EntityMapper.GetMetadata<T>();
        var providerName = GetProviderName(connection);

        // Route to graph insertion if entity has relationships; otherwise use flat insertion
        if (HasGraphRelationships(metadata))
        {
            return ExecuteWithTransactionAsync<int>(
                connection,
                tx,
                (conn, localTx) => Internal.Graph.GraphInsertOrchestrator.InsertGraphAsync(conn, entities, metadata, providerName, localTx, cancellationToken));
        }
        else
        {
            var strategy = ProviderRegistry.Instance.GetBulkCopyStrategy(providerName);
            return ExecuteWithTransactionAsync<int>(
                connection,
                tx,
                async (conn, localTx) => await strategy.BulkInsertAsync(conn, entities, metadata, localTx, cancellationToken));
        }
    }

    /// <summary>
    /// Updates multiple entities in the database in a single batch operation.
    /// </summary>
    /// <typeparam name="T">Entity type to update</typeparam>
    /// <param name="connection">Database connection</param>
    /// <param name="entities">Entities to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows updated</returns>
    public static Task<int> UpdateManyAsync<T>(
        this IDbConnection connection,
        IEnumerable<T> entities,
        IDbTransaction? tx = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        if (entities == null)
            throw new ArgumentNullException(nameof(entities));

        var metadata = EntityMapper.GetMetadata<T>();
        var providerName = GetProviderName(connection);
        var strategy = ProviderRegistry.Instance.GetBulkCopyStrategy(providerName);

        return ExecuteWithTransactionAsync<int>(
            connection,
            tx,
            (conn, localTx) => strategy.BulkUpdateAsync(conn, entities, metadata, localTx, cancellationToken));
    }

    /// <summary>
    /// Deletes multiple entities from the database in a single batch operation.
    /// </summary>
    /// <typeparam name="T">Entity type to delete</typeparam>
    /// <param name="connection">Database connection</param>
    /// <param name="entities">Entities to delete (only key values are used)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows deleted</returns>
    public static Task<int> DeleteManyAsync<T>(
        this IDbConnection connection,
        IEnumerable<T> entities,
        IDbTransaction? tx = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        if (entities == null)
            throw new ArgumentNullException(nameof(entities));

        var metadata = EntityMapper.GetMetadata<T>();
        var providerName = GetProviderName(connection);
        var strategy = ProviderRegistry.Instance.GetBulkCopyStrategy(providerName);

        return ExecuteWithTransactionAsync<int>(
            connection,
            tx,
            (conn, localTx) => strategy.BulkDeleteAsync(conn, entities, metadata, localTx, cancellationToken));
    }

    /// <summary>
    /// Deletes multiple entities from the database by their key values.
    /// </summary>
    /// <typeparam name="T">Entity type to delete</typeparam>
    /// <param name="connection">Database connection</param>
    /// <param name="keys">Key values of entities to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows deleted</returns>
    public static Task<int> DeleteManyAsync<T>(
        this IDbConnection connection,
        IEnumerable<object> keys,
        IDbTransaction? tx = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        if (keys == null)
            throw new ArgumentNullException(nameof(keys));

        var metadata = EntityMapper.GetMetadata<T>();
        var providerName = GetProviderName(connection);
        var strategy = ProviderRegistry.Instance.GetBulkCopyStrategy(providerName);

        return ExecuteWithTransactionAsync<int>(
            connection,
            tx,
            (conn, localTx) => strategy.BulkDeleteByKeysAsync<T>(conn, keys, metadata, localTx, cancellationToken));
    }



    /// <summary>
    /// Executes an operation within a transaction. If externalTransaction is null, opens connection if needed and creates a transaction using optional isolationLevel.
    /// Commits on success and rolls back on exception; disposes created transaction.
    /// </summary>
    private static async Task<T> ExecuteWithTransactionAsync<T>(
        IDbConnection connection,
        IDbTransaction? externalTransaction,
        Func<IDbConnection, IDbTransaction, Task<T>> operation,
        IsolationLevel? isolationLevel = null)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        var localTx = externalTransaction;
        var created = false;
        if (localTx == null)
        {
            if (connection.State != ConnectionState.Open)
                connection.Open();
            localTx = isolationLevel.HasValue ? connection.BeginTransaction(isolationLevel.Value) : connection.BeginTransaction();
            created = true;
        }

        try
        {
            var result = await operation(connection, localTx);
            if (created)
                localTx.Commit();
            return result;
        }
        catch
        {
            if (created)
                localTx.Rollback();
            throw;
        }
        finally
        {
            if (created)
                localTx.Dispose();
        }
    }

    /// <summary>
    /// Determines whether an entity type has graph relationships ([HasMany] or [HasOne] attributes).
    /// </summary>
    private static bool HasGraphRelationships(Internal.Mapping.EntityMetadata metadata)
    {
        return metadata.Relationships.Count > 0;
    }

    /// <summary>
    /// Determines the database provider name from a connection object.
    /// </summary>
    private static string GetProviderName(IDbConnection connection)
    {
        var connectionType = connection.GetType();
        var namespaceName = connectionType.Namespace ?? "";

        return namespaceName switch
        {
            "Microsoft.Data.SqlClient" => "SqlServer",
            "Npgsql" => "PostgreSQL",
            "MySqlConnector" => "MySQL",
            _ => throw new InvalidOperationException(
                $"Unknown database provider: {connectionType.FullName}. " +
                $"Supported providers: SQL Server (Microsoft.Data.SqlClient), " +
                $"PostgreSQL (Npgsql), MySQL (MySqlConnector)")
        };
    }
}
