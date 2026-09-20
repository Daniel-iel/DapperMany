using System.Collections.Concurrent;

namespace DapperMany.Internal.Abstractions;

/// <summary>
/// Default implementation of IProviderRegistry.
/// Thread-safe singleton storage for provider implementations.
/// </summary>
public class ProviderRegistry : IProviderRegistry
{
    private static readonly System.Diagnostics.ActivitySource _activitySource = new("DapperMany.ProviderRegistry");

    private static readonly Lazy<ProviderRegistry> _instance = new(() => new ProviderRegistry());

    private readonly ConcurrentDictionary<string, ProviderImplementation> _providers = new(StringComparer.OrdinalIgnoreCase);

    internal static ProviderRegistry Instance => _instance.Value;
    
    private ProviderRegistry() { }
    
    /// <summary>
    /// Registers a provider globally. Called by provider modules via ModuleInitializer.
    /// </summary>
    public static void RegisterProvider(
        string providerName,
        ISqlDialect dialect,
        IBulkCopyStrategy bulkCopyStrategy,
        IIdentityRetrievalStrategy identityRetrievalStrategy)
    {
        using var activity = _activitySource.StartActivity("RegisterProvider");
        activity?.SetTag("provider.name", providerName);
        _instance.Value.Register(providerName, dialect, bulkCopyStrategy, identityRetrievalStrategy);
    }
    /// <summary>
    /// Registers a provider implementation instance in the registry.
    /// </summary>
    /// <param name="providerName">Name of the provider (e.g., "SqlServer", "PostgreSQL", "MySQL").</param>
    /// <param name="dialect">The SQL dialect implementation for the provider.</param>
    /// <param name="bulkCopyStrategy">The bulk copy strategy implementation for the provider.</param>
    /// <param name="identityRetrievalStrategy">The identity retrieval strategy implementation for the provider.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="providerName"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when any of the strategy parameters are null.</exception>
    public void Register(
        string providerName,
        ISqlDialect dialect,
        IBulkCopyStrategy bulkCopyStrategy,
        IIdentityRetrievalStrategy identityRetrievalStrategy)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name cannot be empty.", nameof(providerName));

        if (dialect == null) throw new ArgumentNullException(nameof(dialect));
        if (bulkCopyStrategy == null) throw new ArgumentNullException(nameof(bulkCopyStrategy));
        if (identityRetrievalStrategy == null) throw new ArgumentNullException(nameof(identityRetrievalStrategy));

        var implementation = new ProviderImplementation(dialect, bulkCopyStrategy, identityRetrievalStrategy);
        _providers.TryAdd(providerName, implementation);
    }

    /// <summary>
    /// Gets the <see cref="ISqlDialect"/> implementation for the specified provider.
    /// </summary>
    /// <param name="providerName">Provider name.</param>
    /// <returns>The dialect implementation.</returns>
    /// <exception cref="InvalidOperationException">If provider is not registered.</exception>
    public ISqlDialect GetDialect(string providerName)
    {
        if (!_providers.TryGetValue(providerName, out var implementation))
            throw new InvalidOperationException($"Provider '{providerName}' is not registered. Available providers: {string.Join(", ", GetRegisteredProviders())}");

        return implementation.Dialect;
    }

    /// <summary>
    /// Gets the <see cref="IBulkCopyStrategy"/> for the specified provider.
    /// </summary>
    /// <param name="providerName">Provider name.</param>
    /// <returns>The bulk copy strategy.</returns>
    /// <exception cref="InvalidOperationException">If provider is not registered.</exception>
    public IBulkCopyStrategy GetBulkCopyStrategy(string providerName)
    {
        if (!_providers.TryGetValue(providerName, out var implementation))
            throw new InvalidOperationException($"Provider '{providerName}' is not registered. Available providers: {string.Join(", ", GetRegisteredProviders())}");

        return implementation.BulkCopyStrategy;
    }

    /// <summary>
    /// Gets the <see cref="IIdentityRetrievalStrategy"/> for the specified provider.
    /// </summary>
    /// <param name="providerName">Provider name.</param>
    /// <returns>The identity retrieval strategy.</returns>
    /// <exception cref="InvalidOperationException">If provider is not registered.</exception>
    public IIdentityRetrievalStrategy GetIdentityRetrievalStrategy(string providerName)
    {
        if (!_providers.TryGetValue(providerName, out var implementation))
            throw new InvalidOperationException($"Provider '{providerName}' is not registered. Available providers: {string.Join(", ", GetRegisteredProviders())}");

        return implementation.IdentityRetrievalStrategy;
    }

    /// <summary>
    /// Returns whether a given provider is registered.
    /// </summary>
    /// <param name="providerName">Provider name.</param>
    public bool IsRegistered(string providerName) => _providers.ContainsKey(providerName);

    /// <summary>
    /// Returns the list of registered provider names.
    /// </summary>
    public IReadOnlyList<string> GetRegisteredProviders() => _providers.Keys.ToList().AsReadOnly();

    /// <summary>
    /// Internal storage for provider implementations.
    /// </summary>
    private record ProviderImplementation(
        ISqlDialect Dialect,
        IBulkCopyStrategy BulkCopyStrategy,
        IIdentityRetrievalStrategy IdentityRetrievalStrategy);
}
