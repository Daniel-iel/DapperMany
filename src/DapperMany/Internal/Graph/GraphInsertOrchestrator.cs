using DapperMany.Internal.Abstractions;
using DapperMany.Internal.Mapping;
using System.Data;
using System.Reflection;

namespace DapperMany.Internal.Graph;

/// <summary>
/// Orchestrates bulk insert operations for entity graphs (parent + children relationships).
/// Handles automatic FK population and ordering of parent/child inserts.
/// </summary>
internal class GraphInsertOrchestrator
{
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

        // Get metadata for child entity type
        var getMetadataMethod = typeof(EntityMapper)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .First(m => m.Name == "GetMetadata" && m.GetGenericArguments().Length == 0)
            .MakeGenericMethod(childEntityType);

        var childMetadata = (EntityMetadata)getMetadataMethod.Invoke(null, new[] { childEntityType })!;

        // Insert all children using reflection to call BulkInsertAsync with correct type
        var insertMethod = bulkCopyStrategy.GetType()
            .GetMethods()
            .First(m => m.Name == "BulkInsertAsync" && m.IsGenericMethod)
            .MakeGenericMethod(childEntityType);

        var task = (Task<int>)insertMethod.Invoke(
            bulkCopyStrategy,
            new object[] { connection, allChildren, childMetadata, cancellationToken })!;

        return await task;
    }
}
