using System.Data;

namespace DapperMany.Internal.Abstractions;

/// <summary>
/// Manages provider registration and resolution.
/// Allows dynamic registration of database providers via ISqlDialect and strategy interfaces.
/// </summary>
public interface IProviderRegistry
{
    /// <summary>
    /// Registers a provider with its dialect and strategies.
    /// Called during application startup by [ModuleInitializer] in provider packages.
    /// </summary>
    /// <param name="providerName">Provider name (e.g., "SqlServer", "PostgreSQL", "MySQL")</param>
    /// <param name="dialect">SQL dialect implementation</param>
    /// <param name="bulkCopyStrategy">Bulk copy strategy implementation</param>
    /// <param name="identityRetrievalStrategy">Identity retrieval strategy implementation</param>
    void Register(
        string providerName,
        ISqlDialect dialect,
        IBulkCopyStrategy bulkCopyStrategy,
        IIdentityRetrievalStrategy identityRetrievalStrategy);

    /// <summary>
    /// Gets the SQL dialect for a specific database provider.
    /// </summary>
    /// <param name="providerName">Provider name</param>
    /// <returns>ISqlDialect implementation for the provider</returns>
    /// <exception cref="InvalidOperationException">If provider is not registered</exception>
    ISqlDialect GetDialect(string providerName);

    /// <summary>
    /// Gets the bulk copy strategy for a specific database provider.
    /// </summary>
    /// <param name="providerName">Provider name</param>
    /// <returns>IBulkCopyStrategy implementation for the provider</returns>
    /// <exception cref="InvalidOperationException">If provider is not registered</exception>
    IBulkCopyStrategy GetBulkCopyStrategy(string providerName);

    /// <summary>
    /// Gets the identity retrieval strategy for a specific database provider.
    /// </summary>
    /// <param name="providerName">Provider name</param>
    /// <returns>IIdentityRetrievalStrategy implementation for the provider</returns>
    /// <exception cref="InvalidOperationException">If provider is not registered</exception>
    IIdentityRetrievalStrategy GetIdentityRetrievalStrategy(string providerName);

    /// <summary>
    /// Checks if a provider is registered.
    /// </summary>
    bool IsRegistered(string providerName);

    /// <summary>
    /// Gets all registered provider names.
    /// </summary>
    IReadOnlyList<string> GetRegisteredProviders();
}
