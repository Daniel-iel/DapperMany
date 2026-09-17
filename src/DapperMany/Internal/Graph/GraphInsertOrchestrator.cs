using DapperMany.Internal.Abstractions;
using DapperMany.Internal.Mapping;
using System.Data;
using System.Reflection;
using System.Diagnostics;

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
    /// <returns>BulkOperationResult with telemetry for parents + all children</returns>
    public static async Task<BulkOperationResult<TParent>> InsertGraphAsync<TParent>(
        IDbConnection connection,
        IEnumerable<TParent> parents,
        EntityMetadata parentMetadata,
        string providerName,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default) where TParent : class
    {
        var parentList = parents.ToList();
        if (parentList.Count == 0)
            return new BulkOperationResult<TParent>();

        var bulkCopyStrategy = ProviderRegistry.Instance.GetBulkCopyStrategy(providerName);
        var identityStrategy = ProviderRegistry.Instance.GetIdentityRetrievalStrategy(providerName);

        // Overall timer for the graph insert
        var overallSw = Stopwatch.StartNew();
        var relatedEntities = new Dictionary<string, int>();
        var allErrors = new List<OperationError>();

        try
        {
            // Step 1: Insert all parent entities
            var swParent = Stopwatch.StartNew();
            var parentsResult = await bulkCopyStrategy.BulkInsertAsync(
                connection, parentList, parentMetadata, transaction, cancellationToken);
            var parentsInserted = parentsResult.RowsInserted;
            swParent.Stop();

            Debug.WriteLine($"[DAPPERMANY] GraphInsert {typeof(TParent).Name} (Parents - {providerName}): affected={parentsInserted}, elapsed={swParent.ElapsedMilliseconds}ms");

            // Collect parent errors if any
            if (!parentsResult.IsSuccessful)
            {
                foreach (var err in parentsResult.Errors)
                {
                    allErrors.Add(err);
                }
            }

            // Capture parent-level generated IDs
            var parentGeneratedIds = parentsResult.GeneratedIds;

            // Step 2: Retrieve generated identity values for parents
            var parentKeyGetter = AccessorFactory.CreateGetter(parentMetadata.KeyProperty!);
            var parentKeySetter = AccessorFactory.CreateSetter(parentMetadata.KeyProperty!);

            // If parents have auto-generated IDs, retrieve them from database
            // For now, assume they were set by the bulk insert operation
            // In a real scenario with SqlServer, use OUTPUT clause to capture IDs

            // Step 3: Process each relationship (insert children with telemetry)
            var totalChildrenInserted = 0;
            foreach (var relationship in parentMetadata.Relationships.Values)
            {
                var childrenResult = await InsertChildrenForRelationship(
                    connection,
                    parentList,
                    parentMetadata,
                    relationship,
                    bulkCopyStrategy,
                    transaction,
                    cancellationToken);

                var childrenInserted = childrenResult.ChildrenCount;
                totalChildrenInserted += childrenInserted;
                relatedEntities[relationship.NavigationProperty?.Name ?? relationship.ForeignKeyPropertyName] = childrenInserted;

                // Collect any errors from children insert
                if (childrenResult.Errors.Count > 0)
                {
                    allErrors.AddRange(childrenResult.Errors);
                }
            }

            overallSw.Stop();
            Debug.WriteLine($"[DAPPERMANY] GraphInsert {typeof(TParent).Name} (Total - {providerName}): parents={parentsInserted}, children={totalChildrenInserted}, elapsed={overallSw.ElapsedMilliseconds}ms");

            // Build final result
            var resultBuilder = BulkOperationResult<TParent>.Builder()
                .WithRowsInserted(parentsInserted + totalChildrenInserted)
                .WithGeneratedIds(parentGeneratedIds)
                .WithDuration(overallSw.Elapsed);

            // Add related entities info
            foreach (var kvp in relatedEntities)
            {
                resultBuilder.WithRelatedEntity(kvp.Key, kvp.Value);
            }

            // Add any errors that occurred
            foreach (var error in allErrors)
            {
                resultBuilder.WithError(error.EntityIndex, error.Message, error.Exception);
            }

            return resultBuilder.Build();
        }
        catch (Exception ex)
        {
            overallSw.Stop();
            return BulkOperationResult<TParent>.Builder()
                .WithDuration(overallSw.Elapsed)
                .WithError(0, $"GraphInsert failed: {ex.Message}", ex)
                .Build();
        }
    }

    private static async Task<(int ChildrenCount, List<OperationError> Errors)> InsertChildrenForRelationship<TParent>(
        IDbConnection connection,
        List<TParent> parents,
        EntityMetadata parentMetadata,
        RelationshipMetadata relationship,
        IBulkCopyStrategy bulkCopyStrategy,
        IDbTransaction? transaction,
        CancellationToken cancellationToken) where TParent : class
    {
        var errors = new List<OperationError>();

        try
        {
            var childEntityType = relationship.ChildEntityType;
            var navigationGetter = AccessorFactory.CreateGetter(relationship.NavigationProperty!);

            // Resolve foreign key property on child type if not already set
            var fkProperty = relationship.ForeignKeyProperty;
            if (fkProperty == null)
            {
                fkProperty = childEntityType.GetProperty(relationship.ForeignKeyPropertyName, BindingFlags.Public | BindingFlags.Instance);
                if (fkProperty == null)
                {
                    // Try to resolve via child metadata as a fallback
                    var childMeta = EntityMapper.GetMetadata(childEntityType);
                    fkProperty = childMeta.MappedProperties.FirstOrDefault(p => string.Equals(p.Name, relationship.ForeignKeyPropertyName, StringComparison.OrdinalIgnoreCase));
                }

                if (fkProperty == null)
                    throw new InvalidOperationException($"Foreign key property '{relationship.ForeignKeyPropertyName}' not resolved on {childEntityType.Name}.");

                relationship.ForeignKeyProperty = fkProperty;
            }

            var fkSetter = AccessorFactory.CreateSetter(fkProperty);
            var parentKeyGetter = AccessorFactory.CreateGetter(parentMetadata.KeyProperty!);

            // Collect all children and populate their FK values (supports collection navigations and single-reference navigations)
            var allChildren = new List<object>();

            foreach (var parent in parents)
            {
                var navValue = navigationGetter(parent);
                if (navValue == null)
                    continue;

                var parentKeyValue = parentKeyGetter(parent);

                // Treat string specially: do not enumerate it
                if (navValue is System.Collections.IEnumerable children && !(navValue is string))
                {
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
                else
                {
                    // Single child scenario (e.g., [HasOne])
                    fkSetter(navValue, parentKeyValue);
                    allChildren.Add(navValue);
                }
            }

            if (allChildren.Count == 0)
                return (0, errors);

            // Get metadata for child entity type
            var childMetadata = EntityMapper.GetMetadata(childEntityType);

            // Insert all children using reflection to call BulkInsertAsync with correct type
            var insertMethod = bulkCopyStrategy.GetType()
                .GetMethods()
                .First(m => m.Name == "BulkInsertAsync" && m.IsGenericMethod)
                .MakeGenericMethod(childEntityType);

            // Convert List<object> to a strongly-typed List<childEntityType> at runtime
            var listType = typeof(System.Collections.Generic.List<>).MakeGenericType(childEntityType);
            var typedList = (System.Collections.IList)Activator.CreateInstance(listType)!;
            foreach (var c in allChildren)
            {
                typedList.Add(c);
            }

            var sw = Stopwatch.StartNew();
            var task = (Task<BulkOperationResult<object>>)insertMethod.Invoke(
                bulkCopyStrategy,
                new object[] { connection, typedList, childMetadata, transaction, cancellationToken })!;

            var childResult = await task;
            sw.Stop();

            var insertedChildren = childResult.RowsInserted;
            Debug.WriteLine($"[DAPPERMANY] GraphInsert Children {childEntityType.Name} (Relationship {relationship.NavigationProperty?.Name ?? relationship.ForeignKeyPropertyName} - {bulkCopyStrategy.GetType().Name}): affected={insertedChildren}, elapsed={sw.ElapsedMilliseconds}ms");

            // Collect any errors from child insert
            if (!childResult.IsSuccessful)
            {
                errors.AddRange(childResult.Errors);
            }

            return (insertedChildren, errors);
        }
        catch (Exception ex)
        {
            errors.Add(new OperationError 
            { 
                EntityIndex = 0,
                Message = $"InsertChildren failed for relationship: {ex.Message}",
                Exception = ex
            });
            return (0, errors);
        }
    }
}
