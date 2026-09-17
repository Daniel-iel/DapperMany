namespace DapperMany.Internal;

/// <summary>
/// Represents the result of a bulk database operation with telemetry information.
/// Provides detailed metrics about rows affected, generated IDs, duration, and any errors that occurred.
/// </summary>
/// <typeparam name="T">The entity type that was operated on</typeparam>
public record BulkOperationResult<T> where T : class
{
    /// <summary>
    /// Number of rows inserted in this operation.
    /// </summary>
    public int RowsInserted { get; init; }

    /// <summary>
    /// Number of rows updated in this operation.
    /// </summary>
    public int RowsUpdated { get; init; }

    /// <summary>
    /// Number of rows deleted in this operation.
    /// </summary>
    public int RowsDeleted { get; init; }

    /// <summary>
    /// Total rows affected (RowsInserted + RowsUpdated + RowsDeleted).
    /// Useful for logging and monitoring bulk operations.
    /// </summary>
    public int TotalRowsAffected => RowsInserted + RowsUpdated + RowsDeleted;

    /// <summary>
    /// List of generated identity values for inserted entities.
    /// For InsertMany operations, this contains the auto-generated keys.
    /// Empty for Update/Delete operations or if no identity column exists.
    /// </summary>
    public IReadOnlyList<object> GeneratedIds { get; init; } = Array.Empty<object>();

    /// <summary>
    /// Dictionary tracking related entity counts by type name.
    /// Example: { "ItemPedido": 12, "PedidoDetalhe": 5 } for inserted/updated child entities.
    /// Useful for understanding graph-based InsertManyGraph operations.
    /// </summary>
    public IReadOnlyDictionary<string, int> RelatedEntities { get; init; } = new Dictionary<string, int>();

    /// <summary>
    /// Duration of the bulk operation from start to completion.
    /// Includes all batch processing time, SQL execution, and result assembly.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// List of errors that occurred during the operation (if any).
    /// Only populated if operation continued despite errors (continueOnError = true).
    /// Empty if no errors occurred or operation failed fast.
    /// </summary>
    public IReadOnlyList<OperationError> Errors { get; init; } = Array.Empty<OperationError>();

    /// <summary>
    /// Indicates whether the operation completed successfully without errors.
    /// True if Errors collection is empty.
    /// </summary>
    public bool IsSuccessful => Errors.Count == 0;

    /// <summary>
    /// Creates a new BulkOperationResult with builder pattern support.
    /// </summary>
    public static BulkOperationResultBuilder<T> Builder() => new();
}

/// <summary>
/// Represents an error that occurred during a bulk operation on a specific entity.
/// </summary>
public record OperationError
{
    /// <summary>
    /// The index of the entity in the original batch that caused the error.
    /// Value of -1 indicates the error is not associated with a specific entity.
    /// </summary>
    public int EntityIndex { get; init; } = -1;

    /// <summary>
    /// The error message describing what went wrong.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// The underlying exception that caused the error, if any.
    /// May be null if error was detected without an exception.
    /// </summary>
    public Exception? Exception { get; init; }
}

/// <summary>
/// Fluent builder for constructing BulkOperationResult<T> instances.
/// Provides a convenient way to build result objects with optional fields.
/// </summary>
public class BulkOperationResultBuilder<T> where T : class
{
    private int _rowsInserted;
    private int _rowsUpdated;
    private int _rowsDeleted;
    private List<object> _generatedIds = new();
    private Dictionary<string, int> _relatedEntities = new();
    private TimeSpan _duration;
    private List<OperationError> _errors = new();

    /// <summary>
    /// Set the number of rows inserted.
    /// </summary>
    public BulkOperationResultBuilder<T> WithRowsInserted(int count)
    {
        _rowsInserted = count;
        return this;
    }

    /// <summary>
    /// Set the number of rows updated.
    /// </summary>
    public BulkOperationResultBuilder<T> WithRowsUpdated(int count)
    {
        _rowsUpdated = count;
        return this;
    }

    /// <summary>
    /// Set the number of rows deleted.
    /// </summary>
    public BulkOperationResultBuilder<T> WithRowsDeleted(int count)
    {
        _rowsDeleted = count;
        return this;
    }

    /// <summary>
    /// Add generated identity values.
    /// </summary>
    public BulkOperationResultBuilder<T> WithGeneratedIds(IEnumerable<object> ids)
    {
        _generatedIds.AddRange(ids);
        return this;
    }

    /// <summary>
    /// Add a single generated identity value.
    /// </summary>
    public BulkOperationResultBuilder<T> WithGeneratedId(object id)
    {
        _generatedIds.Add(id);
        return this;
    }

    /// <summary>
    /// Add related entity count for a given type.
    /// </summary>
    public BulkOperationResultBuilder<T> WithRelatedEntity(string typeName, int count)
    {
        _relatedEntities[typeName] = count;
        return this;
    }

    /// <summary>
    /// Set the duration of the operation.
    /// </summary>
    public BulkOperationResultBuilder<T> WithDuration(TimeSpan duration)
    {
        _duration = duration;
        return this;
    }

    /// <summary>
    /// Add an operation error.
    /// </summary>
    public BulkOperationResultBuilder<T> WithError(OperationError error)
    {
        _errors.Add(error);
        return this;
    }

    /// <summary>
    /// Add an operation error with entity index, message, and optional exception.
    /// </summary>
    public BulkOperationResultBuilder<T> WithError(int entityIndex, string message, Exception? exception = null)
    {
        _errors.Add(new OperationError 
        { 
            EntityIndex = entityIndex,
            Message = message,
            Exception = exception
        });
        return this;
    }

    /// <summary>
    /// Add multiple operation errors.
    /// </summary>
    public BulkOperationResultBuilder<T> WithErrors(IEnumerable<OperationError> errors)
    {
        _errors.AddRange(errors);
        return this;
    }

    /// <summary>
    /// Build the final BulkOperationResult<T> instance.
    /// </summary>
    public BulkOperationResult<T> Build()
    {
        return new BulkOperationResult<T>
        {
            RowsInserted = _rowsInserted,
            RowsUpdated = _rowsUpdated,
            RowsDeleted = _rowsDeleted,
            GeneratedIds = _generatedIds.AsReadOnly(),
            RelatedEntities = new Dictionary<string, int>(_relatedEntities),
            Duration = _duration,
            Errors = _errors.AsReadOnly()
        };
    }
}
