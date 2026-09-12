namespace DapperMany.Attributes;

using System;

/// <summary>
/// Marks a property as a one-to-one relationship to a child entity reference.
/// Used by InsertManyGraphAsync to automatically handle parent-child correlations
/// where the parent holds a single child reference (1:1).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class HasOneAttribute : Attribute
{
    /// <summary>
    /// The name of the foreign key property in the child entity that references this parent.
    /// Must be a writable property on the child entity type.
    /// </summary>
    public string ForeignKey { get; }

    /// <summary>
    /// Initializes a new instance of the HasOneAttribute class.
    /// </summary>
    /// <param name="foreignKey">The property name of the foreign key in the child entity (e.g., "PedidoId" for PedidoDetalhe.PedidoId).</param>
    public HasOneAttribute(string foreignKey)
    {
        if (string.IsNullOrWhiteSpace(foreignKey))
            throw new ArgumentException("Foreign key property name cannot be null or empty.", nameof(foreignKey));

        ForeignKey = foreignKey;
    }
}
