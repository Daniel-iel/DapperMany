namespace DapperMany.Internal.Graph;

/// <summary>
/// Represents a relationship between a parent and child entity type.
/// Used internally for InsertManyGraph operations.
/// </summary>
internal sealed record RelationshipInfo
{
    /// <summary>
    /// The property on the parent entity that holds the collection of children.
    /// </summary>
    public required string NavigationPropertyName { get; init; }

    /// <summary>
    /// The type of elements in the child collection.
    /// </summary>
    public required Type ChildEntityType { get; init; }

    /// <summary>
    /// The name of the foreign key property on the child entity.
    /// </summary>
    public required string ForeignKeyPropertyName { get; init; }

    /// <summary>
    /// The property info for the foreign key on the child entity. Resolved lazily.
    /// </summary>
    public System.Reflection.PropertyInfo? ForeignKeyProperty { get; set; }

    public RelationshipInfo(string navigationPropertyName, Type childEntityType, string foreignKeyPropertyName, System.Reflection.PropertyInfo? foreignKeyProperty)
    {
        NavigationPropertyName = navigationPropertyName;
        ChildEntityType = childEntityType;
        ForeignKeyPropertyName = foreignKeyPropertyName;
        ForeignKeyProperty = foreignKeyProperty;
    }
}
