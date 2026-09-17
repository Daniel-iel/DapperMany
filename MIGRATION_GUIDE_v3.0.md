# DapperMany v3.0 Migration Guide

## Overview
DapperMany v3.0 introduces **breaking changes** to the public API to provide comprehensive telemetry and change tracking for bulk operations. This guide helps you migrate from v2.x to v3.0.

## Key Changes

### 1. Return Type Change: `int` → `BulkOperationResult<T>`

All bulk operation methods now return `BulkOperationResult<T>` instead of `int`, providing detailed telemetry about your operations.

#### Before (v2.x):
```csharp
int rowsInserted = await connection.InsertManyAsync(entities);
Console.WriteLine($"Inserted: {rowsInserted} rows");
```

#### After (v3.0):
```csharp
var result = await connection.InsertManyAsync(entities);
Console.WriteLine($"Inserted: {result.RowsInserted} rows");
Console.WriteLine($"Duration: {result.Duration.TotalMilliseconds}ms");
if (!result.IsSuccessful)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error at entity {error.EntityIndex}: {error.Message}");
    }
}
```

### 2. Affected Methods

The following methods now return `BulkOperationResult<T>`:

| Method | Old Return | New Return | Notes |
|--------|-----------|-----------|-------|
| `InsertManyAsync<T>` | `Task<int>` | `Task<BulkOperationResult<T>>` | Includes generated IDs, duration |
| `UpdateManyAsync<T>` | `Task<int>` | `Task<BulkOperationResult<T>>` | Includes duration, error tracking |
| `DeleteManyAsync<T>` | `Task<int>` | `Task<BulkOperationResult<T>>` | Includes duration |
| `DeleteManyAsync<T>(keys)` | `Task<int>` | `Task<BulkOperationResult<T>>` | Includes duration |
| `InsertManyGraphAsync<T>` | `Task<int>` | `Task<BulkOperationResult<T>>` | NEW: Includes related entity counts |

### 3. BulkOperationResult<T> Properties

```csharp
public record BulkOperationResult<T> where T : class
{
    // Row counts
    public int RowsInserted { get; init; }
    public int RowsUpdated { get; init; }
    public int RowsDeleted { get; init; }
    public int TotalRowsAffected => RowsInserted + RowsUpdated + RowsDeleted;
    
    // Telemetry
    public TimeSpan Duration { get; init; }
    public IReadOnlyList<object> GeneratedIds { get; init; }
    public IReadOnlyDictionary<string, int> RelatedEntities { get; init; }
    
    // Error handling
    public IReadOnlyList<OperationError> Errors { get; init; }
    public bool IsSuccessful => Errors.Count == 0;
}

public record OperationError
{
    public int EntityIndex { get; init; }
    public required string Message { get; init; }
    public Exception? Exception { get; init; }
}
```

## Migration Examples

### InsertMany

**Before (v2.x):**
```csharp
var orders = new[] { /* ... */ };
int inserted = await connection.InsertManyAsync(orders);
```

**After (v3.0):**
```csharp
var orders = new[] { /* ... */ };
var result = await connection.InsertManyAsync(orders);
Console.WriteLine($"Inserted {result.RowsInserted} orders in {result.Duration.TotalMilliseconds}ms");
Console.WriteLine($"Generated IDs: {string.Join(", ", result.GeneratedIds)}");
```

### InsertManyGraph

**Before (v2.x):**
```csharp
var orders = new[]
{
    new Pedido { 
        NumeroDocumento = "PED-001", 
        Itens = new[] { /* ItemPedido */ }
    }
};
int totalInserted = await connection.InsertManyGraphAsync(orders);
```

**After (v3.0):**
```csharp
var orders = new[]
{
    new Pedido { 
        NumeroDocumento = "PED-001", 
        Itens = new[] { /* ItemPedido */ }
    }
};
var result = await connection.InsertManyGraphAsync(orders);
Console.WriteLine($"Inserted {result.RowsInserted} total rows");
Console.WriteLine($"Pedidos: {result.TotalRowsAffected - result.RelatedEntities["ItemPedido"]}");
Console.WriteLine($"ItemPedidos: {result.RelatedEntities["ItemPedido"]}");
```

### UpdateMany

**Before (v2.x):**
```csharp
var orders = new[] { /* modified orders */ };
int updated = await connection.UpdateManyAsync(orders);
```

**After (v3.0):**
```csharp
var orders = new[] { /* modified orders */ };
var result = await connection.UpdateManyAsync(orders);
if (result.IsSuccessful)
{
    Console.WriteLine($"Updated {result.RowsUpdated} rows");
}
else
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Failed to update row {error.EntityIndex}: {error.Message}");
    }
}
```

### DeleteMany

**Before (v2.x):**
```csharp
var orders = new[] { /* orders to delete */ };
int deleted = await connection.DeleteManyAsync(orders);
```

**After (v3.0):**
```csharp
var orders = new[] { /* orders to delete */ };
var result = await connection.DeleteManyAsync(orders);
Console.WriteLine($"Deleted {result.RowsDeleted} rows in {result.Duration.TotalMilliseconds}ms");
```

## Migration Checklist

- [ ] Update method return types in your code to use `BulkOperationResult<T>`
- [ ] Replace simple `int` assignments with `result.RowsInserted/Updated/Deleted` as needed
- [ ] Add telemetry logging using `result.Duration` and `result.Errors`
- [ ] Update error handling: check `result.IsSuccessful` or inspect `result.Errors`
- [ ] For InsertManyGraph: access `result.RelatedEntities` to track child entity counts
- [ ] Review code that relied on simple integer counts for non-breaking changes
- [ ] Test bulk operations to ensure they work correctly with new result types
- [ ] Update any custom wrappers or extension methods that depend on return types

## Backward Compatibility

If you need to temporarily access only the row counts, you can extract them directly:

```csharp
var result = await connection.InsertManyAsync(entities);
int rowsAffected = result.TotalRowsAffected;  // Simplified access
```

## Support

For questions or issues during migration, please refer to:
- [DapperMany Documentation](./docs)
- [GitHub Issues](https://github.com/danieliel/DapperMany/issues)
- [Examples in Samples Project](./samples/DapperMany.Samples)

## What's New in v3.0

### Features
- **Comprehensive Telemetry**: Duration, generated IDs, related entity counts
- **Error Tracking**: Detailed error information per entity
- **Graph Operation Telemetry**: RelatedEntities breakdown for InsertManyGraph
- **Performance Insights**: Measure bulk operation performance
- **Better Observability**: Monitor and debug bulk operations effectively

### Benefits
1. **Performance Monitoring**: Track operation duration for performance optimization
2. **Debugging**: Detailed error messages with entity indices for failing records
3. **Auditing**: Generated ID information for audit trails
4. **Relationship Tracking**: Know exactly how many child entities were inserted/updated
