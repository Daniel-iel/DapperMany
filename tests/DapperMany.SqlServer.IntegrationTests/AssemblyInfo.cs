global using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

// Force module initializer to run by referencing the provider type
_ = typeof(DapperMany.SqlServer.SqlServerProvider);
