namespace DapperMany.Attributes;

using System;

/// <summary>
/// Marks a class as an ORM-mapped entity corresponding to a database table.
/// Reuses System.ComponentModel.DataAnnotations.Schema.TableAttribute for compatibility.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class TableAttribute : Attribute
{
    /// <summary>
    /// The name of the database table.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the TableAttribute class.
    /// </summary>
    /// <param name="name">The name of the database table.</param>
    public TableAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Table name cannot be null or empty.", nameof(name));

        Name = name;
    }
}
