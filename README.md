# DapperMany

Bulk operations for Dapper: fast InsertMany, InsertManyGraph, UpdateMany and DeleteMany across SQL Server, PostgreSQL and MySQL.

## Table of Contents
- **Overview**: What DapperMany is and when to use it.
- **Installation**: How to add the package or build from source.
- **Quick Start**: Minimal examples (InsertMany, InsertManyGraph, UpdateMany, DeleteMany).
- **Mapping Attributes**: `[Table]`, `[Key]`, `[DatabaseGenerated]`, `[HasMany]`.
- **Docker local setup**: Run database stack and connection strings.
- **Providers**: Provider-specific notes (SQL Server, PostgreSQL, MySQL).
- **Running samples and tests**: Commands to run the shipped samples and integration tests.
- **Contributing & License**

## Overview
DapperMany provides a small set of extension methods over `IDbConnection` to perform bulk-style operations with minimal SQL authoring. It aims to keep an API surface small and predictable while supporting provider-specific optimizations (bulk copy strategies) under the hood.

Supported operations (public API):

- `InsertManyAsync<T>(this IDbConnection connection, IEnumerable<T> entities, ...)` — inserts a flat collection or entity graph with auto-detection
- `UpdateManyAsync<T>(this IDbConnection connection, IEnumerable<T> entities, ...)`
- `DeleteManyAsync<T>(this IDbConnection connection, IEnumerable<T> entities, ...)`

See implementation: [src/DapperMany/DapperManyExtensions.cs](src/DapperMany/DapperManyExtensions.cs#L1-L150)

## Installation
Prefer installing the published package when available:

```bash
dotnet add package DapperMany
```

Or build from source and reference the project directly:

```bash
dotnet restore
dotnet build DapperMany.sln -c Release
```

## Quick Start
Below are short examples extracted from the `samples` project.

### Model definitions (attributes)
Example `Pedido` (parent) and `ItemPedido` (child): see [samples/DapperMany.Samples/Models/Pedido.cs](samples/DapperMany.Samples/Models/Pedido.cs#L1-L14) and [samples/DapperMany.Samples/Models/ItemPedido.cs](samples/DapperMany.Samples/Models/ItemPedido.cs#L1-L12)

```csharp
[Table("Pedidos")]
public class Pedido
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public required string NumeroDocumento { get; set; }
    public DateTime DataPedido { get; set; }
    public decimal ValorTotal { get; set; }
    public string Status { get; set; } = "Pendente";

    [HasMany(nameof(ItemPedido.PedidoId))]
    public List<ItemPedido> Itens { get; set; } = new();
}
```

### InsertMany (simple list)
```csharp
var orders = new List<Pedido>
{
    new Pedido { NumeroDocumento = "PED-001", DataPedido = DateTime.UtcNow, ValorTotal = 1500.00m },
    new Pedido { NumeroDocumento = "PED-002", ValorTotal = 2500.00m }
};

var rowsInserted = await connection.InsertManyAsync(orders);
Console.WriteLine($"Inserted {rowsInserted} order(s)");
```

Snippet source: [samples/DapperMany.Samples/Program.cs](samples/DapperMany.Samples/Program.cs#L158-L172)

### InsertManyGraph (parents + children)
```csharp
var orders = new List<Pedido>
{
    new Pedido
    {
        NumeroDocumento = "PED-GRAPH-001",
        ValorTotal = 1200.00m,
        Itens = new()
        {
            new ItemPedido { Descricao = "Laptop", Quantidade = 1, ValorUnitario = 800m },
            new ItemPedido { Descricao = "Mouse", Quantidade = 2, ValorUnitario = 50m }
        }
    }
};

int insertedCount = await connection.InsertManyAsync(orders);
// Parent IDs and children FK values are populated by the library automatically
```

Snippet source: [samples/DapperMany.Samples/Program.cs](samples/DapperMany.Samples/Program.cs#L246-L261)

### UpdateMany
```csharp
var existingOrders = connection.Query<Pedido>(
    "SELECT TOP 3 * FROM Pedidos ORDER BY Id DESC").ToList();

foreach (var order in existingOrders)
    order.Status = "Processado";

var updatedCount = await connection.UpdateManyAsync(existingOrders);
Console.WriteLine($"Updated {updatedCount} orders");
```

Snippet source: [samples/DapperMany.Samples/Program.cs](samples/DapperMany.Samples/Program.cs#L319-L342)

### DeleteMany
```csharp
var ordersToDelete = connection.Query<Pedido>(
    "SELECT TOP 2 * FROM Pedidos ORDER BY Id ASC").ToList();

var deleteCount = await connection.DeleteManyAsync(ordersToDelete);
Console.WriteLine($"Deleted {deleteCount} orders");
```

Snippet source: [samples/DapperMany.Samples/Program.cs](samples/DapperMany.Samples/Program.cs#L361-L374)

## Mapping Attributes
- `[Table("Name")]` — maps a CLR class to a DB table.
- `[Key]` — marks the primary key property.
- `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` — identity/auto-increment.
- `[HasMany("ForeignKeyName")]` — declares a 1:N collection and allows `InsertManyGraph` to populate child FKs.

See definitions: [src/DapperMany/Attributes](src/DapperMany/Attributes/)

## Docker local setup
A docker compose file that brings up SQL Server, PostgreSQL and MySQL is included under `docker/`.

```bash
# start databases
docker compose -f docker/docker-compose.yml up -d

# stop and remove
docker compose -f docker/docker-compose.yml down
```

Default connection strings (sample project):

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost,1433;Initial Catalog=DapperMany;User Id=sa;Password=SqlServer123!;Encrypt=false;",
    "PostgreSQL": "Host=localhost;Port=5432;Database=dappermany;Username=postgres;Password=Postgres123!;",
    "MySQL": "Server=localhost;Port=3306;Database=dappermany;Uid=root;Pwd=MySql123!;"
  }
}
```

DB schema initialization scripts are located at:

- [docker/sqlserver/init/01-schema.sql](docker/sqlserver/init/01-schema.sql#L1-L50)
- [docker/postgres/init/01-schema.sql](docker/postgres/init/01-schema.sql#L1-L40)
- [docker/mysql/init/01-schema.sql](docker/mysql/init/01-schema.sql#L1-L35)

## Providers
Provider-specific optimizations are implemented under `src/`:

- [src/DapperMany.SqlServer/SqlServerBulkCopyStrategy.cs](src/DapperMany.SqlServer/SqlServerBulkCopyStrategy.cs#L1-L200)
- [src/DapperMany.Postgres/PostgreSqlBulkCopyStrategy.cs](src/DapperMany.Postgres/PostgreSqlBulkCopyStrategy.cs#L1-L200)
- [src/DapperMany.MySql/MySqlBulkCopyStrategy.cs](src/DapperMany.MySql/MySqlBulkCopyStrategy.cs#L1-L200)

Provider notes and design decisions are recorded in [specs/spec.md](specs/spec.md)

## Diagnostics

In Debug builds the library emits lightweight diagnostics for bulk operations to the debug output using `System.Diagnostics.Debug.WriteLine()` along with a `Stopwatch` measurement. Messages follow the format:

`[DAPPERMANY] <Op> <Entity> (<Provider>): affected=<N>, elapsed=<Tms>ms`

These logs are produced only in `#if DEBUG` builds and are intended for local troubleshooting. They are emitted by provider implementations and the graph orchestrator (not by public extension methods).

## Running the samples and tests
Make sure Docker is running the local DBs before executing integration tests or the sample project.

```bash
docker compose -f docker/docker-compose.yml up -d
dotnet restore
dotnet build DapperMany.sln -c Release
dotnet run --project samples/DapperMany.Samples

# to run a specific integration test project (SQL Server tests require a running SQL Server)
dotnet test tests/DapperMany.SqlServer.IntegrationTests -c Release
```

## Integration tests
Integration tests live under `tests/` and validate `InsertManyGraph`, `UpdateMany` and `DeleteMany` behavior. Example:

- [tests/DapperMany.SqlServer.IntegrationTests/InsertManyGraphIntegrationTests.cs](tests/DapperMany.SqlServer.IntegrationTests/InsertManyGraphIntegrationTests.cs#L1-L50)

## Contributing
- Fork, create a feature branch, open a PR.
- Run unit tests: `dotnet test tests/DapperMany.UnitTests`.

## License
This repository is licensed under the terms in `LICENSE`.

---

If you'd like, I can now:

1. Run `dotnet build` and `dotnet test` locally to verify the repository builds and that the README snippets compile. (Requires Docker for integration tests.)
2. Open a follow-up PR with the README and small edits to `docker/README.md`.

Pick one and I'll proceed.