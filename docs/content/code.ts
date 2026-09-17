export const CODE = {
  heroUsage: `await connection.InsertManyGraphAsync(orders);
await connection.UpdateManyAsync(partialOrders);
await connection.DeleteManyAsync(orderIds);`,

  entityModel: `[Table("Orders")]
public class Order
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Customer { get; set; }
    public DateTime OrderDate { get; set; }

    [HasMany(foreignKey: nameof(OrderItem.OrderId))]
    public List<OrderItem> Items { get; set; }
}

[Table("OrderItems")]
public class OrderItem
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int OrderId { get; set; }
    public string Product { get; set; }
}`,

  quickstartUsage: `await connection.InsertManyGraphAsync(orders);    // parent + children, FK resolved automatically
await connection.UpdateManyAsync(partialOrders);  // object with just [Key] + fields to update
await connection.DeleteManyAsync(orderIds);`,

  entityModelWithDetail: `[Table("Orders")]
public class Order
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Customer { get; set; }
    public DateTime OrderDate { get; set; }

    [HasMany(foreignKey: nameof(OrderItem.OrderId))]
    public List<OrderItem> Items { get; set; }

    // 1:1 relationship — single reference property
    [HasOne(foreignKey: nameof(OrderDetail.OrderId))]
    public OrderDetail Detail { get; set; }
}`,

  publicApi: `public static class DbConnectionExtensions
{
    Task InsertManyAsync<T>(this IDbConnection cn, IEnumerable<T> entities, IDbTransaction? tx = null);
    Task InsertManyGraphAsync<T>(this IDbConnection cn, IEnumerable<T> entities, IDbTransaction? tx = null);
    Task UpdateManyAsync<T>(this IDbConnection cn, IEnumerable<T> entities, IDbTransaction? tx = null);
    Task DeleteManyAsync<T>(this IDbConnection cn, IEnumerable<T> entities, IDbTransaction? tx = null);
    Task DeleteManyAsync<T>(this IDbConnection cn, IEnumerable<object> keys, IDbTransaction? tx = null);
}`,

  edgeNull: `var order = new Order { DocumentNumber = "ORD-NULL", Items = null };
await connection.InsertManyGraphAsync(new[] { order }); // Inserts only the parent`,

  edgeHasOne: `var order = new Order { DocumentNumber = "ORD-DET", Detail = new OrderDetail { /* ... */ } };
await connection.InsertManyGraphAsync(new[] { order }); // Inserts parent + detail (1:1)`,

  deleteManyCall: `await connection.DeleteManyAsync(orderIds);`,

  txUsage: `using var tx = connection.BeginTransaction();
await connection.InsertManyGraphAsync(orders, tx);
tx.Commit();`,

  moduleInit: `internal static class SqlServerModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize() =>
        ProviderRegistry.Register("Microsoft.Data.SqlClient.SqlConnection",
            new ProviderModule(new SqlServerDialect(), new SqlServerBulkCopyStrategy(), new SqlServerIdentityStrategy()));
}`,

  loggingExample: `[DAPPERMANY] BulkInsert Order (SqlServer): affected=10, elapsed=45ms`,

  installSqlServer: `dotnet add package DapperMany
dotnet add package DapperMany.SqlServer`,

  installPostgres: `dotnet add package DapperMany
dotnet add package DapperMany.Postgres`,

  installMySql: `dotnet add package DapperMany
dotnet add package DapperMany.MySql`,

  telemetryBasic: `var result = await connection.InsertManyAsync(orders);
Console.WriteLine($"Inserted {result.RowsInserted} orders in {result.Duration.TotalMilliseconds}ms");
Console.WriteLine($"Generated IDs: {string.Join(", ", result.GeneratedIds)}");`,

  telemetryGraph: `var result = await connection.InsertManyGraphAsync(orders);
Console.WriteLine($"Total rows inserted: {result.TotalRowsAffected}");
if (result.RelatedEntities.TryGetValue("OrderItem", out var itemCount))
{
    Console.WriteLine($"  Orders: {result.RowsInserted - itemCount}");
    Console.WriteLine($"  Items: {itemCount}");
}`,

  errorTracking: `var result = await connection.UpdateManyAsync(orders);
if (!result.IsSuccessful)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error at index {error.EntityIndex}: {error.Message}");
        if (error.Exception != null)
            Console.WriteLine($"  {error.Exception}");
    }
}`,
};
