namespace DapperMany.Internal.Mapping;

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using DapperMany.Attributes;

/// <summary>
/// Extracts ORM metadata from entity types by reading attributes.
/// Builds and caches EntityMetadata with thread-safe lazy initialization.
/// </summary>
public static class EntityMapper
{
    private static readonly ConcurrentDictionary<Type, Lazy<EntityMetadata>> _metadataCache = new();

    /// <summary>
    /// Gets or creates EntityMetadata for a type, with thread-safe lazy initialization.
    /// </summary>
    /// <typeparam name="T">The entity type to map.</typeparam>
    /// <returns>Immutable EntityMetadata for type T.</returns>
    /// <exception cref="InvalidOperationException">If the type is missing required attributes.</exception>
    public static EntityMetadata GetMetadata<T>() where T : class
    {
        return GetMetadata(typeof(T));
    }

    /// <summary>
    /// Gets or creates EntityMetadata for a type, with thread-safe lazy initialization.
    /// </summary>
    /// <param name="entityType">The entity type to map.</param>
    /// <returns>Immutable EntityMetadata for the type.</returns>
    /// <exception cref="InvalidOperationException">If the type is missing required attributes.</exception>
    public static EntityMetadata GetMetadata(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);

        var lazy = _metadataCache.GetOrAdd(
            entityType,
            t => new Lazy<EntityMetadata>(
                () => BuildMetadata(t),
                LazyThreadSafetyMode.ExecutionAndPublication
            )
        );

        return lazy.Value;
    }

    private static EntityMetadata BuildMetadata(Type entityType)
    {
        // Read [Table] attribute (using standard System.ComponentModel.DataAnnotations.Schema)
        var tableAttr = entityType.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>()
            ?? throw new InvalidOperationException(
                $"Type '{entityType.FullName}' is missing the [Table] attribute.");

        // Find [Key] property (using standard System.ComponentModel.DataAnnotations)
        var keyProperty = entityType.GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<System.ComponentModel.DataAnnotations.KeyAttribute>() != null)
            ?? throw new InvalidOperationException(
                $"Type '{entityType.FullName}' has no [Key] property.");

        // Collect all mapped properties (all public scalar properties excluding [NotMapped] and navigation properties)
        var mappedProperties = entityType
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => p.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute>() == null)
            // Exclude navigation properties marked with HasMany/HasOne (they are handled as relationships)
            .Where(p => p.GetCustomAttribute<DapperMany.Attributes.HasManyAttribute>() == null && p.GetCustomAttribute<DapperMany.Attributes.HasOneAttribute>() == null)
            .ToList();

        if (!mappedProperties.Contains(keyProperty))
            mappedProperties.Insert(0, keyProperty);

        // Identify identity properties (using standard System.ComponentModel.DataAnnotations.Schema)
        var identityProperties = mappedProperties
            .Where(p => p.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute>()?.DatabaseGeneratedOption 
                == System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)
            .ToList();

        // Build relationships from [HasMany] and [HasOne] properties (using custom DapperMany.Attributes)
        var relationships = new Dictionary<string, RelationshipMetadata>();
        foreach (var prop in entityType.GetProperties())
        {
            // HasMany (collection navigation)
            var hasMany = prop.GetCustomAttribute<HasManyAttribute>();
            if (hasMany != null)
            {
                // Get the child type from the collection property
                var childType = GetCollectionElementType(prop.PropertyType)
                    ?? throw new InvalidOperationException(
                        $"Property '{prop.Name}' on '{entityType.FullName}' must be a collection type (e.g., List<T>, IEnumerable<T>).");

                relationships[prop.Name] = new RelationshipMetadata
                {
                    NavigationProperty = prop,
                    ChildEntityType = childType,
                    ForeignKeyPropertyName = hasMany.ForeignKey
                };

                continue;
            }

            // HasOne (single reference navigation)
            var hasOne = prop.GetCustomAttribute<HasOneAttribute>();
            if (hasOne != null)
            {
                var childType = prop.PropertyType;
                relationships[prop.Name] = new RelationshipMetadata
                {
                    NavigationProperty = prop,
                    ChildEntityType = childType,
                    ForeignKeyPropertyName = hasOne.ForeignKey
                };

                continue;
            }
        }

        var metadata = new EntityMetadata
        {
            EntityType = entityType,
            TableName = tableAttr.Name,
            KeyProperty = keyProperty,
            MappedProperties = mappedProperties.AsReadOnly(),
            IdentityProperties = identityProperties.AsReadOnly(),
            Relationships = new System.Collections.ObjectModel.ReadOnlyDictionary<string, RelationshipMetadata>(relationships)
        };

        metadata.Validate();
        return metadata;
    }

    private static Type? GetCollectionElementType(Type type)
    {
        // Handle List<T>, IEnumerable<T>, ICollection<T>, etc.
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(List<>) || 
                genericDef == typeof(IList<>) || 
                genericDef == typeof(ICollection<>) ||
                genericDef == typeof(IEnumerable<>))
            {
                return type.GetGenericArguments()[0];
            }
        }

        return null;
    }

    /// <summary>
    /// Clears all cached metadata. Useful for testing or dynamic scenarios.
    /// </summary>
    internal static void ClearCache()
    {
        _metadataCache.Clear();
    }
}

/// <summary>
/// Marks a property as not mapped to any database column.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class NotMappedAttribute : Attribute
{
}
