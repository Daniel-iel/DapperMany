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
    /// </summary>
    /// <typeparam name="T">Entity type to insert</typeparam>
    /// <param name="connection">Database connection</param>
    /// <param name="entities">Entities to insert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows inserted</returns>
    /// <exception cref="InvalidOperationException">If the entity type has no [Table] attribute or provider is not registered</exception>
    public static async Task<int> InsertManyAsync<T>(
        this IDbConnection connection,
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default) where T : class
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (entities == null) throw new ArgumentNullException(nameof(entities));

        var metadata = EntityMapper.GetMetadata<T>();
        var providerName = GetProviderName(connection);
        var strategy = ProviderRegistry.Instance.GetBulkCopyStrategy(providerName);

        return await strategy.BulkInsertAsync(connection, entities, metadata, cancellationToken);
    }

    /// <summary>
    /// Updates multiple entities in the database in a single batch operation.
    /// </summary>
    /// <typeparam name="T">Entity type to update</typeparam>
    /// <param name="connection">Database connection</param>
    /// <param name="entities">Entities to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows updated</returns>
    public static async Task<int> UpdateManyAsync<T>(
        this IDbConnection connection,
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default) where T : class
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (entities == null) throw new ArgumentNullException(nameof(entities));

        var metadata = EntityMapper.GetMetadata<T>();
        var providerName = GetProviderName(connection);
        var strategy = ProviderRegistry.Instance.GetBulkCopyStrategy(providerName);

        return await strategy.BulkUpdateAsync(connection, entities, metadata, cancellationToken);
    }

    /// <summary>
    /// Deletes multiple entities from the database in a single batch operation.
    /// </summary>
    /// <typeparam name="T">Entity type to delete</typeparam>
    /// <param name="connection">Database connection</param>
    /// <param name="entities">Entities to delete (only key values are used)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows deleted</returns>
    public static async Task<int> DeleteManyAsync<T>(
        this IDbConnection connection,
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default) where T : class
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (entities == null) throw new ArgumentNullException(nameof(entities));

        var metadata = EntityMapper.GetMetadata<T>();
        var providerName = GetProviderName(connection);
        var strategy = ProviderRegistry.Instance.GetBulkCopyStrategy(providerName);

        return await strategy.BulkDeleteAsync(connection, entities, metadata, cancellationToken);
    }

    /// <summary>
    /// Deletes multiple entities from the database by their key values.
    /// </summary>
    /// <typeparam name="T">Entity type to delete</typeparam>
    /// <param name="connection">Database connection</param>
    /// <param name="keys">Key values of entities to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows deleted</returns>
    public static async Task<int> DeleteManyAsync<T>(
        this IDbConnection connection,
        IEnumerable<object> keys,
        CancellationToken cancellationToken = default) where T : class
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (keys == null) throw new ArgumentNullException(nameof(keys));

        var metadata = EntityMapper.GetMetadata<T>();
        var providerName = GetProviderName(connection);
        var strategy = ProviderRegistry.Instance.GetBulkCopyStrategy(providerName);

        return await strategy.BulkDeleteByKeysAsync<T>(connection, keys, metadata, cancellationToken);
    }

    /// <summary>
    /// Inserts multiple parent entities with their related child entities (graph insert).
    /// Automatically populates foreign key values in children after parents are inserted.
    /// Children are identified via [HasMany] attributes on parent entity properties.
    /// </summary>
    /// <typeparam name="T">Parent entity type</typeparam>
    /// <param name="connection">Database connection</param>
    /// <param name="entities">Parent entities with populated child collections</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total number of rows inserted (parents + all children)</returns>
    /// <remarks>
    /// Usage example:
    /// var orders = new List&lt;Pedido&gt;
    /// {
    ///     new Pedido { NumeroDocumento = "PED-001", Itens = new List&lt;ItemPedido&gt; 
    ///     {
    ///         new ItemPedido { Descricao = "Item 1", Quantidade = 1, ValorUnitario = 100 }
    ///     }}
    /// };
    /// var totalInserted = await connection.InsertManyGraphAsync(orders);
    /// // Pedido.Id will be populated from database
    /// // ItemPedido.PedidoId will be auto-populated from Pedido.Id
    /// </remarks>
    public static async Task<int> InsertManyGraphAsync<T>(
        this IDbConnection connection,
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default) where T : class
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (entities == null) throw new ArgumentNullException(nameof(entities));

        var metadata = EntityMapper.GetMetadata<T>();
        var providerName = GetProviderName(connection);

        return await Internal.Graph.GraphInsertOrchestrator.InsertGraphAsync(
            connection, entities, metadata, providerName, cancellationToken);
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
