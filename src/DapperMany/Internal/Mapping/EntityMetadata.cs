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
    /// The primary key property. Must be present exactly once.
    /// </summary>
    public required PropertyInfo KeyProperty { get; init; }

    /// <summary>
    /// All mapped properties (including key property) that correspond to table columns.
    /// </summary>
    public required IReadOnlyList<PropertyInfo> MappedProperties { get; init; }

    /// <summary>
    /// Properties marked with [DatabaseGenerated(Identity)], typically auto-increment columns.
    /// </summary>
    public required IReadOnlyList<PropertyInfo> IdentityProperties { get; init; }

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

        if (KeyProperty == null)
            throw new InvalidOperationException($"No [Key] property found on {EntityType.Name}.");

        if (!MappedProperties.Contains(KeyProperty))
            throw new InvalidOperationException("Key property must be included in MappedProperties.");
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
