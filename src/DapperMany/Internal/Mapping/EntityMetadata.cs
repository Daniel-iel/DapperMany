namespace DapperMany.Internal.Mapping;

using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Immutable metadata about a mapped entity type extracted from attributes.
/// All collections are exposed as read-only for thread-safety.
/// </summary>
public sealed record EntityMetadata
{
    /// <summary>
    /// The entity type being mapped.
    /// </summary>
    public required Type EntityType { get; init; }

    /// <summary>
    /// The database table name.
    /// </summary>
    public required string TableName { get; init; }

    /// <summary>
    /// The primary key properties. Can be one (single) or multiple (composite).
    /// Must contain at least one property.
    /// </summary>
    public required IReadOnlyList<PropertyInfo> KeyProperties { get; init; }

    /// <summary>
    /// Computed property indicating whether this entity has a composite key.
    /// </summary>
    public bool IsCompositeKey => KeyProperties.Count > 1;

    /// <summary>
    /// Backward compatibility: returns the first (and only) key property for single-key entities.
    /// </summary>
    public PropertyInfo KeyProperty => KeyProperties[0];

    /// <summary>
    /// All mapped properties (including key properties) that correspond to table columns.
    /// </summary>
    public required IReadOnlyList<PropertyInfo> MappedProperties { get; init; }

    /// <summary>
    /// Properties marked with [DatabaseGenerated(Identity)], typically auto-increment columns.
    /// </summary>
    public required IReadOnlyList<PropertyInfo> IdentityProperties { get; init; }

    /// <summary>
    /// Set-based view of identity properties for fast membership checks during bulk operations.
    /// </summary>
    public required IReadOnlySet<PropertyInfo> IdentityPropertySet { get; init; }

    /// <summary>
    /// Indicates whether the key property is database-generated.
    /// </summary>
    public bool HasIdentityKey => IdentityPropertySet.Contains(KeyProperty);

    /// <summary>
    /// Relationships defined via [HasMany], keyed by the property name on the parent entity.
    /// </summary>
    public required IReadOnlyDictionary<string, RelationshipMetadata> Relationships { get; init; }

    /// <summary>
    /// Validates the metadata for consistency. Throws on invalid state.
    /// </summary>
    public void Validate()
    {
        if (EntityType == null)
            throw new InvalidOperationException("EntityType is required.");

        if (string.IsNullOrWhiteSpace(TableName))
            throw new InvalidOperationException("TableName is required.");

        if (KeyProperties == null || KeyProperties.Count == 0)
            throw new InvalidOperationException($"No [Key] properties found on {EntityType.Name}.");

        foreach (var keyProp in KeyProperties)
        {
            if (!MappedProperties.Contains(keyProp))
                throw new InvalidOperationException($"Key property '{keyProp.Name}' must be included in MappedProperties.");
            
            // Composite key cannot have auto-generated identity
            if (IsCompositeKey && IdentityProperties.Contains(keyProp))
                throw new InvalidOperationException(
                    $"Entity '{EntityType.Name}' has a composite key with property '{keyProp.Name}' marked as [DatabaseGenerated(Identity)]. " +
                    "Composite keys cannot have auto-generated properties.");
        }
    }
}

/// <summary>
/// Metadata about a one-to-many relationship.
/// </summary>
public sealed record RelationshipMetadata
{
    /// <summary>
    /// The property on the parent entity that holds the collection.
    /// </summary>
    public required PropertyInfo NavigationProperty { get; init; }

    /// <summary>
    /// The element type of the collection (e.g., ItemPedido for List&lt;ItemPedido&gt;).
    /// </summary>
    public required Type ChildEntityType { get; init; }

    /// <summary>
    /// The property name in the child entity that holds the foreign key.
    /// </summary>
    public required string ForeignKeyPropertyName { get; init; }

    /// <summary>
    /// The resolved foreign key property on the child entity.
    /// Resolved lazily to avoid circular dependency issues.
    /// </summary>
    public PropertyInfo? ForeignKeyProperty { get; set; }
}
