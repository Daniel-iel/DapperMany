namespace DapperMany.Attributes;

using System;

/// <summary>
/// Marks a property as the primary key of an entity.
/// Reuses System.ComponentModel.DataAnnotations.KeyAttribute for compatibility.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class KeyAttribute : Attribute
{
}
