using System.Diagnostics;
using DapperMany.Samples.Models;
using Npgsql;
using Testcontainers.PostgreSql;

namespace DapperMany.Postgres.IntegrationTests;

/// <summary>
/// Integration tests for InsertManyGraph using PostgreSQL via Testcontainers or external connection.
/// </summary>
public class InsertManyGraphIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private string? _connectionString;

    public async Task InitializeAsync()
    {
        var provided = Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTIONSTRING");
        if (!string.IsNullOrWhiteSpace(provided))
        {
            _connectionString = provided;
        }
        else
        {
            var pgPassword = Environment.GetEnvironmentVariable("TEST_POSTGRES_PASSWORD") ?? "Postgres123!";
            _container = new PostgreSqlBuilder()
                .WithDatabase("dappermany")
                .WithUsername("postgres")
                .WithPassword(pgPassword)
                .Build();

            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();
        }

        // Ensure provider registered
        DapperMany.Postgres.PostgreSqlProvider.Register();

        // Wait for readiness
        var ready = false;
        var sw = Stopwatch.StartNew();
        var timeout = TimeSpan.FromMinutes(5);
        while (sw.Elapsed < timeout)
        {
            try
            {
                using var testConn = new NpgsqlConnection(_connectionString);
                await testConn.OpenAsync();
                await testConn.CloseAsync();
                ready = true;
                break;
            }
            catch
            {
                await Task.Delay(2000);
            }
        }

        if (!ready)
            throw new InvalidOperationException($"Postgres did not become ready within {timeout.TotalMinutes} minutes.");

        await InitializeSchema();
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
            await _container.StopAsync();
    }

    private async Task InitializeSchema()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd1 = connection.CreateCommand();
        cmd1.CommandText = @"
            CREATE TABLE IF NOT EXISTS ""Pedidos"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""NumeroDocumento"" VARCHAR(50) NOT NULL UNIQUE,
                ""DataPedido"" TIMESTAMP NOT NULL,
                ""ValorTotal"" NUMERIC(18,2) NOT NULL,
                ""Status"" VARCHAR(50) NOT NULL,
                ""Created"" TIMESTAMP NOT NULL,
                ""Modified"" TIMESTAMP NOT NULL
            );
        ";
        await cmd1.ExecuteNonQueryAsync();

        using var cmd2 = connection.CreateCommand();
        cmd2.CommandText = @"
            CREATE TABLE IF NOT EXISTS ""ItensPedido"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""PedidoId"" INTEGER NOT NULL,
                ""Descricao"" VARCHAR(255) NOT NULL,
                ""Quantidade"" INTEGER NOT NULL,
                ""ValorUnitario"" NUMERIC(18,2) NOT NULL,
                ""ValorTotal"" NUMERIC(18,2) NOT NULL,
                ""Created"" TIMESTAMP NOT NULL,
                ""Modified"" TIMESTAMP NOT NULL,
                FOREIGN KEY (""PedidoId"") REFERENCES ""Pedidos""(""Id"") ON DELETE CASCADE
            );
        ";
        await cmd2.ExecuteNonQueryAsync();

        using var cleanup = connection.CreateCommand();
        cleanup.CommandText = @"TRUNCATE TABLE ""ItensPedido"", ""Pedidos"" RESTART IDENTITY CASCADE;";
        await cleanup.ExecuteNonQueryAsync();

        // Ensure ValorTotal column exists on ItensPedido (for running docker-compose DBs that may be outdated)
        using var alter = connection.CreateCommand();
        alter.CommandText = @"ALTER TABLE ""ItensPedido"" ADD COLUMN IF NOT EXISTS ""ValorTotal"" NUMERIC(18,2) NOT NULL DEFAULT 0;";
        await alter.ExecuteNonQueryAsync();
        using var alterCreated = connection.CreateCommand();
        alterCreated.CommandText = @"ALTER TABLE ""ItensPedido"" ADD COLUMN IF NOT EXISTS ""Created"" TIMESTAMP NOT NULL DEFAULT now();";
        await alterCreated.ExecuteNonQueryAsync();
        using var alterModified = connection.CreateCommand();
        alterModified.CommandText = @"ALTER TABLE ""ItensPedido"" ADD COLUMN IF NOT EXISTS ""Modified"" TIMESTAMP NOT NULL DEFAULT now();";
        await alterModified.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task InsertManyGraph_SingleParentWithChildren_PopulatesForeignKeys()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var pedido = new Pedido
        {
            NumeroDocumento = "PED-001",
            DataPedido = DateTime.UtcNow,
            ValorTotal = 1000m,
            Status = "Pendente",
            Itens = new()
            {
                new ItemPedido { Descricao = "Item 1", Quantidade = 2, ValorUnitario = 250m },
                new ItemPedido { Descricao = "Item 2", Quantidade = 3, ValorUnitario = 100m }
            }
        };

        var insertedCount = await connection.InsertManyGraphAsync(new[] { pedido });

        Assert.Equal(1, insertedCount);

        using var selectParent = connection.CreateCommand();
        selectParent.CommandText = "SELECT COUNT(*) FROM \"Pedidos\" WHERE \"NumeroDocumento\" = 'PED-001';";
        var parentCount = Convert.ToInt32(await selectParent.ExecuteScalarAsync());
        Assert.Equal(1, parentCount);

        using var selectChildren = connection.CreateCommand();
        selectChildren.CommandText = "SELECT COUNT(*) FROM \"ItensPedido\" WHERE \"PedidoId\" = (SELECT \"Id\" FROM \"Pedidos\" WHERE \"NumeroDocumento\" = 'PED-001');";
        var childCount = Convert.ToInt32(await selectChildren.ExecuteScalarAsync());
        Assert.Equal(2, childCount);
    }

    [Fact]
    public async Task InsertManyGraph_MultipleParentsWithVaryingChildCounts_AllInsertsSucceed()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var pedidos = new[]
        {
            new Pedido
            {
                NumeroDocumento = "PED-A",
                DataPedido = DateTime.UtcNow,
                ValorTotal = 500m,
                Status = "Pendente",
                Itens = new()
                {
                    new ItemPedido { Descricao = "Item A1", Quantidade = 1, ValorUnitario = 500m }
                }
            },
            new Pedido
            {
                NumeroDocumento = "PED-B",
                DataPedido = DateTime.UtcNow,
                ValorTotal = 1500m,
                Status = "Processado",
                Itens = new()
                {
                    new ItemPedido { Descricao = "Item B1", Quantidade = 2, ValorUnitario = 300m },
                    new ItemPedido { Descricao = "Item B2", Quantidade = 1, ValorUnitario = 900m },
                    new ItemPedido { Descricao = "Item B3", Quantidade = 3, ValorUnitario = 100m }
                }
            },
            new Pedido
            {
                NumeroDocumento = "PED-C",
                DataPedido = DateTime.UtcNow,
                ValorTotal = 0m,
                Status = "Cancelado",
                Itens = new() // Empty
            }
        };

        var insertedCount = await connection.InsertManyGraphAsync(pedidos);

        Assert.Equal(3, insertedCount);

        using var selectParents = connection.CreateCommand();
        selectParents.CommandText = "SELECT COUNT(*) FROM \"Pedidos\" WHERE \"NumeroDocumento\" IN ('PED-A','PED-B','PED-C');";
        var parentCount = Convert.ToInt32(await selectParents.ExecuteScalarAsync());
        Assert.Equal(3, parentCount);

        using var selectChildrenA = connection.CreateCommand();
        selectChildrenA.CommandText = "SELECT COUNT(*) FROM \"ItensPedido\" WHERE \"PedidoId\" = (SELECT \"Id\" FROM \"Pedidos\" WHERE \"NumeroDocumento\" = 'PED-A');";
        var childCountA = Convert.ToInt32(await selectChildrenA.ExecuteScalarAsync());
        Assert.Equal(1, childCountA);

        using var selectChildrenB = connection.CreateCommand();
        selectChildrenB.CommandText = "SELECT COUNT(*) FROM \"ItensPedido\" WHERE \"PedidoId\" = (SELECT \"Id\" FROM \"Pedidos\" WHERE \"NumeroDocumento\" = 'PED-B');";
        var childCountB = Convert.ToInt32(await selectChildrenB.ExecuteScalarAsync());
        Assert.Equal(3, childCountB);

        using var selectChildrenC = connection.CreateCommand();
        selectChildrenC.CommandText = "SELECT COUNT(*) FROM \"ItensPedido\" WHERE \"PedidoId\" = (SELECT \"Id\" FROM \"Pedidos\" WHERE \"NumeroDocumento\" = 'PED-C');";
        var childCountC = Convert.ToInt32(await selectChildrenC.ExecuteScalarAsync());
        Assert.Equal(0, childCountC);
    }

    [Fact]
    public async Task InsertManyGraph_NullChildren_InsertsParentOnly()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var pedido = new Pedido
        {
            NumeroDocumento = "PED-NULL",
            DataPedido = DateTime.UtcNow,
            ValorTotal = 100m,
            Status = "Pendente",
            Itens = null
        };

        var insertedCount = await connection.InsertManyGraphAsync(new[] { pedido });
        Assert.Equal(1, insertedCount);

        using var selectParent = connection.CreateCommand();
        selectParent.CommandText = "SELECT COUNT(*) FROM \"Pedidos\" WHERE \"NumeroDocumento\" = 'PED-NULL';";
        var parentCount = Convert.ToInt32(await selectParent.ExecuteScalarAsync());
        Assert.Equal(1, parentCount);

        using var selectChildren = connection.CreateCommand();
        selectChildren.CommandText = "SELECT COUNT(*) FROM \"ItensPedido\" WHERE \"PedidoId\" = (SELECT \"Id\" FROM \"Pedidos\" WHERE \"NumeroDocumento\" = 'PED-NULL');";
        var childCount = Convert.ToInt32(await selectChildren.ExecuteScalarAsync());
        Assert.Equal(0, childCount);
    }

    [Fact]
    public async Task InsertManyGraph_ChildForeignKeyAutoPopulated_MatchesParentId()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var pedido = new Pedido
        {
            NumeroDocumento = "PED-FK-TEST",
            DataPedido = DateTime.UtcNow,
            ValorTotal = 250m,
            Status = "Processado",
            Itens = new()
            {
                new ItemPedido { Descricao = "FK Test Item", Quantidade = 1, ValorUnitario = 250m }
            }
        };

        var insertedCount = await connection.InsertManyGraphAsync(new[] { pedido });
        Assert.Equal(1, insertedCount);

        using var selectFK = connection.CreateCommand();
        selectFK.CommandText = @"
            SELECT i.""PedidoId""
            FROM ""ItensPedido"" i
            INNER JOIN ""Pedidos"" p ON i.""PedidoId"" = p.""Id""
            WHERE p.""NumeroDocumento"" = 'PED-FK-TEST'";
        var insertedFK = Convert.ToInt32(await selectFK.ExecuteScalarAsync());
        Assert.True(insertedFK > 0);

        using var selectParentId = connection.CreateCommand();
        selectParentId.CommandText = @"SELECT ""Id"" FROM ""Pedidos"" WHERE ""NumeroDocumento"" = 'PED-FK-TEST'";
        var parentId = Convert.ToInt32(await selectParentId.ExecuteScalarAsync());
        Assert.Equal(parentId, insertedFK);
    }
}
