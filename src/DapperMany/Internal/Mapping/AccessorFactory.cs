namespace DapperMany.Internal.Mapping;

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

/// <summary>
/// Factory for creating compiled getters and setters via Expression Trees.
/// Dramatically faster than PropertyInfo.GetValue/SetValue at runtime.
/// Caches compiled delegates globally.
/// </summary>
public static class AccessorFactory
{
    private static readonly ConcurrentDictionary<PropertyInfo, Delegate> _getterCache = new();
    private static readonly ConcurrentDictionary<PropertyInfo, Delegate> _setterCache = new();

    /// <summary>
    /// Creates a compiled getter for a property. Results are cached globally.
    /// </summary>
    /// <param name="property">The property to get.</param>
    /// <returns>A Func&lt;object, object?&gt; getter that extracts the property value.</returns>
    public static Func<object, object?> CreateGetter(PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);

        if (!_getterCache.TryGetValue(property, out var cached))
        {
            var getter = CompileGetter(property);
            _getterCache.TryAdd(property, getter);
            return (Func<object, object?>)getter;
        }

        return (Func<object, object?>)cached;
    }

    /// <summary>
    /// Creates a compiled setter for a property. Results are cached globally.
    /// </summary>
    /// <param name="property">The property to set.</param>
    /// <returns>An Action&lt;object, object?&gt; setter that sets the property value.</returns>
    public static Action<object, object?> CreateSetter(PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);

        if (!_setterCache.TryGetValue(property, out var cached))
        {
            var setter = CompileSetter(property);
            _setterCache.TryAdd(property, setter);
            return (Action<object, object?>)setter;
        }

        return (Action<object, object?>)cached;
    }

    private static Func<object, object?> CompileGetter(PropertyInfo property)
    {
        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var instanceCast = Expression.Convert(instanceParam, property.DeclaringType!);
        var propertyAccess = Expression.Property(instanceCast, property);
        var boxed = Expression.Convert(propertyAccess, typeof(object));
        var lambda = Expression.Lambda<Func<object, object?>>(boxed, instanceParam);
        return lambda.Compile();
    }

    private static Action<object, object?> CompileSetter(PropertyInfo property)
    {
        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var valueParam = Expression.Parameter(typeof(object), "value");

        var instanceCast = Expression.Convert(instanceParam, property.DeclaringType!);
        var valueCast = Expression.Convert(valueParam, property.PropertyType);
        var assignment = Expression.Assign(
            Expression.Property(instanceCast, property),
            valueCast
        );

        var lambda = Expression.Lambda<Action<object, object?>>(assignment, instanceParam, valueParam);
        return lambda.Compile();
    }

    /// <summary>
    /// Clears all cached getters and setters. Useful for testing or dynamic scenarios.
    /// </summary>
    internal static void ClearCache()
    {
        _getterCache.Clear();
        _setterCache.Clear();
    }
}
