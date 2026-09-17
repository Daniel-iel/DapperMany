using System.Collections.Concurrent;

namespace DapperMany.Internal.Observability;

/// <summary>
/// Stores telemetry data for a bulk operation in progress.
/// Allows strategies to record detailed metrics without changing interface signatures.
/// </summary>
public class BulkOperationTelemetry
{
    public int RowsInserted { get; set; }
    public int RowsUpdated { get; set; }
    public int RowsDeleted { get; set; }
    public List<object> GeneratedIds { get; set; } = new();
    public Dictionary<string, int> RelatedEntities { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Thread-safe context for tracking telemetry across async bulk operations.
/// Uses AsyncLocal to maintain context flow across await points.
/// </summary>
public static class TelemetryContext
{
    private static readonly AsyncLocal<Stack<BulkOperationTelemetry>> _telemetryStack = new();
    private static readonly ConcurrentDictionary<long, BulkOperationTelemetry> _completedOperations = new();
    private static long _operationCounter = 0;

    /// <summary>
    /// Begin a new telemetry tracking context for a bulk operation.
    /// Returns an IDisposable that must be disposed when operation completes.
    /// </summary>
    public static (IDisposable Scope, long OperationId) BeginOperation()
    {
        var stack = _telemetryStack.Value ??= new Stack<BulkOperationTelemetry>();
        var telemetry = new BulkOperationTelemetry();
        stack.Push(telemetry);

        var operationId = Interlocked.Increment(ref _operationCounter);

        return (new TelemetryScope(stack, operationId, telemetry), operationId);
    }

    /// <summary>
    /// Get the current operation's telemetry context (if any).
    /// </summary>
    public static BulkOperationTelemetry? GetCurrentTelemetry()
    {
        var stack = _telemetryStack.Value;
        return stack?.Count > 0 ? stack.Peek() : null;
    }

    /// <summary>
    /// Retrieve completed operation telemetry by operation ID.
    /// </summary>
    public static bool TryGetCompletedOperation(long operationId, out BulkOperationTelemetry? telemetry)
    {
        return _completedOperations.TryGetValue(operationId, out telemetry);
    }

    /// <summary>
    /// Clear all stored completed operations (useful for testing).
    /// </summary>
    public static void ClearCompletedOperations()
    {
        _completedOperations.Clear();
    }

    private class TelemetryScope : IDisposable
    {
        private readonly Stack<BulkOperationTelemetry> _stack;
        private readonly long _operationId;
        private readonly BulkOperationTelemetry _telemetry;
        private bool _disposed;

        public TelemetryScope(Stack<BulkOperationTelemetry> stack, long operationId, BulkOperationTelemetry telemetry)
        {
            _stack = stack;
            _operationId = operationId;
            _telemetry = telemetry;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _stack.Pop();

            // Store completed operation for later retrieval
            _completedOperations.TryAdd(_operationId, _telemetry);
        }
    }
}
