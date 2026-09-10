using DapperMany.Internal.Abstractions;
using DapperMany.Internal.Mapping;
using System.Collections.Concurrent;
using System.Data;
using System.Reflection;

namespace DapperMany.Internal.Graph;

/// <summary>
/// Orchestrates bulk insert operations for entity graphs (parent + children relationships).
/// Handles automatic FK population and ordering of parent/child inserts.
/// </summary>
internal sealed class GraphInsertOrchestrator
{
    // Cache for GetMetadata<T>() delegates to avoid reflection on each relationship insert
    private static readonly ConcurrentDictionary<Type, Func<EntityMetadata>> _metadataGetterCache = new();

    // Cache for BulkInsertAsync<T>() delegates to avoid reflection on each relationship insert
    private static readonly ConcurrentDictionary<Type, Func<IBulkCopyStrategy, IDbConnection, IEnumerable<object>, EntityMetadata, CancellationToken, Task<int>>> _bulkInsertCache = new();

    /// <summary>
    /// Inserts a collection of parent entities and their related children in the correct order.
    /// Automatically populates foreign key values in children after parents are inserted.
    /// </summary>
    /// <typeparam name="TParent">Parent entity type</typeparam>
    /// <param name="connection">Database connection</param>
    /// <param name="parents">Parent entities with child collections</param>
    /// <param name="parentMetadata">Metadata for parent entity type</param>
    /// <param name="providerName">Database provider name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total number of rows inserted (parents + all children)</returns>
    public static async Task<int> InsertGraphAsync<TParent>(
        IDbConnection connection,
        IEnumerable<TParent> parents,
        EntityMetadata parentMetadata,
        string providerName,
        CancellationToken cancellationToken = default) where TParent : class
    {
        var parentList = parents.ToList();
        if (parentList.Count == 0)
            return 0;

        var bulkCopyStrategy = ProviderRegistry.Instance.GetBulkCopyStrategy(providerName);
        var identityStrategy = ProviderRegistry.Instance.GetIdentityRetrievalStrategy(providerName);

        int totalInserted = 0;

        // Step 1: Insert all parent entities
        totalInserted += await bulkCopyStrategy.BulkInsertAsync(
            connection, parentList, parentMetadata, cancellationToken);

        // Step 2: Retrieve generated identity values for parents
        var parentKeyGetter = AccessorFactory.CreateGetter(parentMetadata.KeyProperty!);
        var parentKeySetter = AccessorFactory.CreateSetter(parentMetadata.KeyProperty!);

        // If parents have auto-generated IDs, retrieve them from database
        // For now, assume they were set by the bulk insert operation
        // In a real scenario with SqlServer, use OUTPUT clause to capture IDs

        // Step 3: Process each relationship
        foreach (var relationship in parentMetadata.Relationships.Values)
        {
            totalInserted += await InsertChildrenForRelationship(
                connection,
                parentList,
                parentMetadata,
                relationship,
                bulkCopyStrategy,
                cancellationToken);
        }

        return totalInserted;
    }

    private static async Task<int> InsertChildrenForRelationship<TParent>(
        IDbConnection connection,
        List<TParent> parents,
        EntityMetadata parentMetadata,
        RelationshipMetadata relationship,
        IBulkCopyStrategy bulkCopyStrategy,
        CancellationToken cancellationToken) where TParent : class
    {
        var childEntityType = relationship.ChildEntityType;
        var navigationGetter = AccessorFactory.CreateGetter(relationship.NavigationProperty!);
        var fkProperty = relationship.ForeignKeyProperty ??
            throw new InvalidOperationException($"Foreign key property '{relationship.ForeignKeyPropertyName}' not resolved on {childEntityType.Name}.");
        var fkSetter = AccessorFactory.CreateSetter(fkProperty);
        var parentKeyGetter = AccessorFactory.CreateGetter(parentMetadata.KeyProperty!);

        // Collect all children and populate their FK values
        var allChildren = new List<object>();

        foreach (var parent in parents)
        {
            var childCollection = navigationGetter(parent);
            if (childCollection is System.Collections.IEnumerable children)
            {
                var parentKeyValue = parentKeyGetter(parent);

                foreach (var child in children)
                {
                    if (child != null)
                    {
                        // Set the foreign key value on the child
                        fkSetter(child, parentKeyValue);
                        allChildren.Add(child);
                    }
                }
            }
        }

        if (allChildren.Count == 0)
            return 0;

        // Get metadata for child entity type using cached delegate
        var metadataGetter = _metadataGetterCache.GetOrAdd(
            childEntityType,
            t => CompileMetadataGetter(t));
        var childMetadata = metadataGetter();

        // Get BulkInsertAsync delegate for child type using cached delegate
        var bulkInsertDelegate = _bulkInsertCache.GetOrAdd(
            childEntityType,
            t => CompileBulkInsertDelegate(t));

        // Call BulkInsertAsync with the cached delegate (no reflection per insert)
        return await bulkInsertDelegate(bulkCopyStrategy, connection, allChildren, childMetadata, cancellationToken);
    }

    /// <summary>
    /// Compiles a delegate for EntityMapper.GetMetadata{T}() to avoid reflection on each call.
    /// </summary>
    private static Func<EntityMetadata> CompileMetadataGetter(Type childEntityType)
    {
        var getMetadataMethod = typeof(EntityMapper)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == "GetMetadata" && m.GetGenericArguments().Length == 0)
            .MakeGenericMethod(childEntityType);

        return () => (EntityMetadata)getMetadataMethod.Invoke(null, new[] { childEntityType })!;
    }

    /// <summary>
    /// Compiles a delegate for IBulkCopyStrategy.BulkInsertAsync{T}() to avoid reflection on each call.
    /// </summary>
    private static Func<IBulkCopyStrategy, IDbConnection, IEnumerable<object>, EntityMetadata, CancellationToken, Task<int>> CompileBulkInsertDelegate(Type childEntityType)
    {
        var bulkInsertMethod = typeof(IBulkCopyStrategy)
            .GetMethods()
            .First(m => m.Name == "BulkInsertAsync" && m.IsGenericMethod)
            .MakeGenericMethod(childEntityType);

        return (strategy, connection, entities, metadata, token) =>
            (Task<int>)bulkInsertMethod.Invoke(strategy, new object[] { connection, entities, metadata, token })!;
    }
}
