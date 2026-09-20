export const CODE = {
  heroUsage: `await connection.InsertManyAsync(orders); // inserts flat or graph
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

  quickstartUsage: `await connection.InsertManyAsync(orders);    // parent + children, FK resolved automatically
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
    Task UpdateManyAsync<T>(this IDbConnection cn, IEnumerable<T> entities, IDbTransaction? tx = null);
    Task DeleteManyAsync<T>(this IDbConnection cn, IEnumerable<T> entities, IDbTransaction? tx = null);    
}`,

  edgeNull: `var order = new Order { DocumentNumber = "ORD-NULL", Items = null };
await connection.InsertManyAsync(new[] { order }); // Inserts only the parent`,

  edgeHasOne: `var order = new Order { DocumentNumber = "ORD-DET", Detail = new OrderDetail { /* ... */ } };
await connection.InsertManyAsync(new[] { order }); // Inserts parent + detail (1:1)`,

  deleteManyCall: `await connection.DeleteManyAsync(orderIds);`,

  txUsage: `using var tx = connection.BeginTransaction();
await connection.InsertManyAsync(orders, tx);
tx.Commit();`,

  moduleInit: `internal static class SqlServerModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize() =>
        ProviderRegistry.Register("Microsoft.Data.SqlClient.SqlConnection",
            new ProviderModule(new SqlServerDialect(), new SqlServerBulkCopyStrategy(), new SqlServerIdentityStrategy()));
}`,

  loggingExample: `[DAPPERMANY] BulkInsert Order (SqlServer): affected=10, elapsed=45ms`,

  installSqlServer: `dotnet add package DMany
dotnet add package DMany.SqlServer`,

  installPostgres: `dotnet add package DMany
dotnet add package DMany.Postgres`,

  installMySql: `dotnet add package DMany
dotnet add package DMany.MySql`,
};
