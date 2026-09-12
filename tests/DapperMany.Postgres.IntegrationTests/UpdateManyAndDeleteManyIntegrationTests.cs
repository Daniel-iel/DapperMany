using System.Diagnostics;
using Dapper;
using DapperMany.Samples.Models;
using Npgsql;
using Testcontainers.PostgreSql;

namespace DapperMany.Postgres.IntegrationTests;

public class UpdateManyAndDeleteManyIntegrationTests : IAsyncLifetime
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

        DapperMany.Postgres.PostgreSqlProvider.Register();

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
                ""ValorTotal"" NUMERIC(18,2) NOT NULL DEFAULT 0,
                ""Created"" TIMESTAMP NOT NULL DEFAULT now(),
                ""Modified"" TIMESTAMP NOT NULL DEFAULT now(),
                FOREIGN KEY (""PedidoId"") REFERENCES ""Pedidos""(""Id"")
            );
        ";
        await cmd2.ExecuteNonQueryAsync();

        using var cleanup = connection.CreateCommand();
        cleanup.CommandText = @"TRUNCATE TABLE ""ItensPedido"", ""Pedidos"" RESTART IDENTITY CASCADE;";
        await cleanup.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task UpdateMany_UpdatesMultipleEntities()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var pedidos = new List<Pedido>
        {
            new Pedido { NumeroDocumento = "UPD-001", ValorTotal = 100m, Status = "Pendente" },
            new Pedido { NumeroDocumento = "UPD-002", ValorTotal = 200m, Status = "Pendente" },
            new Pedido { NumeroDocumento = "UPD-003", ValorTotal = 300m, Status = "Pendente" }
        };

        var insertCount = await connection.InsertManyAsync(pedidos);
        Assert.Equal(3, insertCount);

        var inserted = await connection.QueryAsync<Pedido>("SELECT * FROM \"Pedidos\" WHERE \"NumeroDocumento\" LIKE 'UPD-%'");
        var pedidoList = inserted.ToList();
        Assert.Equal(3, pedidoList.Count);

        var updatedPedidos = pedidoList.Select(p => new Pedido
        {
            Id = p.Id,
            NumeroDocumento = p.NumeroDocumento,
            Status = "Processado"
        }).ToList();

        var updateCount = await connection.UpdateManyAsync(updatedPedidos);
        Assert.Equal(3, updateCount);

        var afterUpdate = await connection.QueryAsync<Pedido>("SELECT * FROM \"Pedidos\" WHERE \"NumeroDocumento\" LIKE 'UPD-%' ORDER BY \"Id\"");
        var verifyList = afterUpdate.ToList();
        Assert.All(verifyList, p => Assert.Equal("Processado", p.Status));
    }

    [Fact]
    public async Task UpdateMany_PartialUpdate_OnlyModifiesSpecifiedColumns()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var pedido = new Pedido { NumeroDocumento = "PARTIAL-UPDATE", ValorTotal = 500m, Status = "Pendente" };
        var insertCount = await connection.InsertManyAsync(new[] { pedido });
        Assert.Equal(1, insertCount);

        var inserted = (await connection.QueryAsync<Pedido>("SELECT * FROM \"Pedidos\" WHERE \"NumeroDocumento\" = 'PARTIAL-UPDATE'")).First();

        var partialUpdate = new Pedido { Id = inserted.Id, NumeroDocumento = inserted.NumeroDocumento, Status = "Cancelado" };
        var updateCount = await connection.UpdateManyAsync(new[] { partialUpdate });
        Assert.Equal(1, updateCount);

        var updated = (await connection.QueryAsync<Pedido>("SELECT * FROM \"Pedidos\" WHERE \"Id\" = @id", new { id = inserted.Id })).First();
        Assert.Equal("Cancelado", updated.Status);
        Assert.Equal(500m, updated.ValorTotal);
    }

    [Fact]
    public async Task DeleteMany_DeletesMultipleEntities()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var pedidos = new List<Pedido>
        {
            new Pedido { NumeroDocumento = "DEL-001", ValorTotal = 100m },
            new Pedido { NumeroDocumento = "DEL-002", ValorTotal = 200m },
            new Pedido { NumeroDocumento = "DEL-003", ValorTotal = 300m }
        };

        await connection.InsertManyAsync(pedidos);

        var inserted = (await connection.QueryAsync<Pedido>("SELECT * FROM \"Pedidos\" WHERE \"NumeroDocumento\" LIKE 'DEL-%'")).ToList();
        Assert.Equal(3, inserted.Count);

        var deleteCount = await connection.DeleteManyAsync(inserted);
        Assert.Equal(3, deleteCount);

        var remaining = await connection.QueryAsync<Pedido>("SELECT * FROM \"Pedidos\" WHERE \"NumeroDocumento\" LIKE 'DEL-%'");
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task DeleteManyByKeys_DeletesEntitiesByKeyValuesOnly()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var pedidos = new List<Pedido>
        {
            new Pedido { NumeroDocumento = "DELKEY-001", ValorTotal = 100m },
            new Pedido { NumeroDocumento = "DELKEY-002", ValorTotal = 200m },
            new Pedido { NumeroDocumento = "DELKEY-003", ValorTotal = 300m }
        };

        await connection.InsertManyAsync(pedidos);

        var inserted = (await connection.QueryAsync<Pedido>("SELECT * FROM \"Pedidos\" WHERE \"NumeroDocumento\" LIKE 'DELKEY-%'")).ToList();
        var ids = inserted.Select(p => (object)p.Id).ToList();
        Assert.Equal(3, ids.Count);

        var deleteCount = await connection.DeleteManyAsync<Pedido>(ids);
        Assert.Equal(3, deleteCount);

        var remaining = await connection.QueryAsync<Pedido>("SELECT * FROM \"Pedidos\" WHERE \"NumeroDocumento\" LIKE 'DELKEY-%'");
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task UpdateMany_WithChildEntities_DoesNotAffectChildren()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var pedido = new Pedido
        {
            NumeroDocumento = "CHILD-TEST",
            ValorTotal = 1000m,
            Status = "Pendente",
            Itens = new() { new ItemPedido { Descricao = "Item 1", Quantidade = 2, ValorUnitario = 500m } }
        };

        await connection.InsertManyGraphAsync(new[] { pedido });

        var inserted = (await connection.QueryAsync<Pedido>("SELECT * FROM \"Pedidos\" WHERE \"NumeroDocumento\" = 'CHILD-TEST'")).First();

        var update = new Pedido { Id = inserted.Id, NumeroDocumento = inserted.NumeroDocumento, Status = "Processado" };
        var updateCount = await connection.UpdateManyAsync(new[] { update });
        Assert.Equal(1, updateCount);

        var afterUpdate = (await connection.QueryAsync<Pedido>("SELECT * FROM \"Pedidos\" WHERE \"Id\" = @id", new { id = inserted.Id })).First();
        Assert.Equal("Processado", afterUpdate.Status);

        var children = await connection.QueryAsync<ItemPedido>("SELECT * FROM \"ItensPedido\" WHERE \"PedidoId\" = @id", new { id = inserted.Id });
        Assert.Single(children);
    }

    [Fact]
    public async Task DeleteMany_CascadeToChildren_MustBeExplicitForV1()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var pedido = new Pedido
        {
            NumeroDocumento = "CASCADE-TEST",
            ValorTotal = 500m,
            Itens = new() { new ItemPedido { Descricao = "Item 1", Quantidade = 1, ValorUnitario = 500m } }
        };

        await connection.InsertManyGraphAsync(new[] { pedido });

        var insertedOrder = (await connection.QueryAsync<Pedido>("SELECT * FROM \"Pedidos\" WHERE \"NumeroDocumento\" = 'CASCADE-TEST'")).First();
        var orderId = insertedOrder.Id;

        var children = (await connection.QueryAsync<ItemPedido>("SELECT * FROM \"ItensPedido\" WHERE \"PedidoId\" = @id", new { id = orderId })).ToList();
        await connection.DeleteManyAsync(children);

        var parent = (await connection.QueryAsync<Pedido>("SELECT * FROM \"Pedidos\" WHERE \"Id\" = @id", new { id = orderId })).ToList();
        var deleteCount = await connection.DeleteManyAsync(parent);

        Assert.Equal(1, deleteCount);

        var remainingParent = await connection.QueryAsync<Pedido>("SELECT * FROM \"Pedidos\" WHERE \"Id\" = @id", new { id = orderId });
        Assert.Empty(remainingParent);
    }
}
