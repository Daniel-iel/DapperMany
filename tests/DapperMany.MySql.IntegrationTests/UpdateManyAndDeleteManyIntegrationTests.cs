using System.Diagnostics;
using Dapper;
using DapperMany.Samples.Models;
using MySqlConnector;
using Testcontainers.MySql;

namespace DapperMany.MySql.IntegrationTests;

public class UpdateManyAndDeleteManyIntegrationTests : IAsyncLifetime
{
    private MySqlContainer? _container;
    private string? _connectionString;

    public async Task InitializeAsync()
    {
        var provided = Environment.GetEnvironmentVariable("TEST_MYSQL_CONNECTIONSTRING");
        if (!string.IsNullOrWhiteSpace(provided))
        {
            _connectionString = provided;
        }
        else
        {
            var rootPwd = Environment.GetEnvironmentVariable("TEST_MYSQL_ROOT_PASSWORD") ?? "MySql123!";
            _container = new MySqlBuilder()
                .WithDatabase("dappermany")
                .WithUsername("root")
                .WithPassword(rootPwd)
                .Build();

            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();
        }

        DapperMany.MySql.MySqlProvider.Register();

        var ready = false;
        var sw = Stopwatch.StartNew();
        var timeout = TimeSpan.FromMinutes(5);
        while (sw.Elapsed < timeout)
        {
            try
            {
                using var testConn = new MySqlConnection(_connectionString);
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
            throw new InvalidOperationException($"MySQL did not become ready within {timeout.TotalMinutes} minutes.");

        await InitializeSchema();
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
            await _container.StopAsync();
    }

    private async Task InitializeSchema()
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Pedidos (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                NumeroDocumento VARCHAR(50) NOT NULL UNIQUE,
                DataPedido DATETIME NOT NULL,
                ValorTotal DECIMAL(18,2) NOT NULL,
                Status VARCHAR(50) NOT NULL,
                Created DATETIME NOT NULL,
                Modified DATETIME NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ItensPedido (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                PedidoId INT NOT NULL,
                Descricao VARCHAR(255) NOT NULL,
                Quantidade INT NOT NULL,
                ValorUnitario DECIMAL(18,2) NOT NULL,
                ValorTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
                Created DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                Modified DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                CONSTRAINT FK_ItensPedido_Pedidos FOREIGN KEY (PedidoId) REFERENCES Pedidos(Id)
            );
        ";
        await cmd.ExecuteNonQueryAsync();

        using var cleanup = connection.CreateCommand();
        cleanup.CommandText = @"
            SET FOREIGN_KEY_CHECKS=0;
            TRUNCATE TABLE ItensPedido;
            TRUNCATE TABLE Pedidos;
            SET FOREIGN_KEY_CHECKS=1;
        ";
        await cleanup.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task UpdateMany_UpdatesMultipleEntities()
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var pedidos = new List<Pedido>
        {
            new Pedido { NumeroDocumento = "UPD-001", ValorTotal = 100m, Status = "Pendente" },
            new Pedido { NumeroDocumento = "UPD-002", ValorTotal = 200m, Status = "Pendente" },
            new Pedido { NumeroDocumento = "UPD-003", ValorTotal = 300m, Status = "Pendente" }
        };

        var insertCount = await connection.InsertManyAsync(pedidos);
        Assert.Equal(3, insertCount);

        var inserted = await connection.QueryAsync<Pedido>("SELECT * FROM Pedidos WHERE NumeroDocumento LIKE 'UPD-%'");
        var pedidoList = inserted.ToList();
        Assert.Equal(3, pedidoList.Count);

        var updatedPedidos = pedidoList.Select(p => new Pedido { Id = p.Id, NumeroDocumento = p.NumeroDocumento, Status = "Processado" }).ToList();
        var updateCount = await connection.UpdateManyAsync(updatedPedidos);
        Assert.Equal(3, updateCount);

        var afterUpdate = await connection.QueryAsync<Pedido>("SELECT * FROM Pedidos WHERE NumeroDocumento LIKE 'UPD-%' ORDER BY Id");
        var verifyList = afterUpdate.ToList();
        Assert.All(verifyList, p => Assert.Equal("Processado", p.Status));
    }

    [Fact]
    public async Task UpdateMany_PartialUpdate_OnlyModifiesSpecifiedColumns()
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var pedido = new Pedido { NumeroDocumento = "PARTIAL-UPDATE", ValorTotal = 500m, Status = "Pendente" };
        var insertCount = await connection.InsertManyAsync(new[] { pedido });
        Assert.Equal(1, insertCount);

        var inserted = (await connection.QueryAsync<Pedido>("SELECT * FROM Pedidos WHERE NumeroDocumento = 'PARTIAL-UPDATE' ")).First();

        var partialUpdate = new Pedido { Id = inserted.Id, NumeroDocumento = inserted.NumeroDocumento, Status = "Cancelado" };
        var updateCount = await connection.UpdateManyAsync(new[] { partialUpdate });
        Assert.Equal(1, updateCount);

        var updated = (await connection.QueryAsync<Pedido>("SELECT * FROM Pedidos WHERE Id = @id", new { id = inserted.Id })).First();
        Assert.Equal("Cancelado", updated.Status);
        Assert.Equal(500m, updated.ValorTotal);
    }

    [Fact]
    public async Task DeleteMany_DeletesMultipleEntities()
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var pedidos = new List<Pedido>
        {
            new Pedido { NumeroDocumento = "DEL-001", ValorTotal = 100m },
            new Pedido { NumeroDocumento = "DEL-002", ValorTotal = 200m },
            new Pedido { NumeroDocumento = "DEL-003", ValorTotal = 300m }
        };

        await connection.InsertManyAsync(pedidos);

        var inserted = (await connection.QueryAsync<Pedido>("SELECT * FROM Pedidos WHERE NumeroDocumento LIKE 'DEL-%' ")).ToList();
        Assert.Equal(3, inserted.Count);

        var deleteCount = await connection.DeleteManyAsync(inserted);
        Assert.Equal(3, deleteCount);

        var remaining = await connection.QueryAsync<Pedido>("SELECT * FROM Pedidos WHERE NumeroDocumento LIKE 'DEL-%' ");
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task DeleteManyByKeys_DeletesEntitiesByKeyValuesOnly()
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var pedidos = new List<Pedido>
        {
            new Pedido { NumeroDocumento = "DELKEY-001", ValorTotal = 100m },
            new Pedido { NumeroDocumento = "DELKEY-002", ValorTotal = 200m },
            new Pedido { NumeroDocumento = "DELKEY-003", ValorTotal = 300m }
        };

        await connection.InsertManyAsync(pedidos);

        var inserted = (await connection.QueryAsync<Pedido>("SELECT * FROM Pedidos WHERE NumeroDocumento LIKE 'DELKEY-%' ")).ToList();
        var ids = inserted.Select(p => (object)p.Id).ToList();
        Assert.Equal(3, ids.Count);

        var deleteCount = await connection.DeleteManyAsync<Pedido>(ids);
        Assert.Equal(3, deleteCount);

        var remaining = await connection.QueryAsync<Pedido>("SELECT * FROM Pedidos WHERE NumeroDocumento LIKE 'DELKEY-%' ");
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task UpdateMany_WithChildEntities_DoesNotAffectChildren()
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var pedido = new Pedido { NumeroDocumento = "CHILD-TEST", ValorTotal = 1000m, Status = "Pendente", Itens = new() { new ItemPedido { Descricao = "Item 1", Quantidade = 2, ValorUnitario = 500m } } };
        await connection.InsertManyGraphAsync(new[] { pedido });

        var inserted = (await connection.QueryAsync<Pedido>("SELECT * FROM Pedidos WHERE NumeroDocumento = 'CHILD-TEST' ")).First();

        var update = new Pedido { Id = inserted.Id, NumeroDocumento = inserted.NumeroDocumento, Status = "Processado" };
        var updateCount = await connection.UpdateManyAsync(new[] { update });
        Assert.Equal(1, updateCount);

        var afterUpdate = (await connection.QueryAsync<Pedido>("SELECT * FROM Pedidos WHERE Id = @id", new { id = inserted.Id })).First();
        Assert.Equal("Processado", afterUpdate.Status);

        var children = await connection.QueryAsync<ItemPedido>("SELECT * FROM ItensPedido WHERE PedidoId = @id", new { id = inserted.Id });
        Assert.Single(children);
    }

    [Fact]
    public async Task DeleteMany_CascadeToChildren_MustBeExplicitForV1()
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var pedido = new Pedido { NumeroDocumento = "CASCADE-TEST", ValorTotal = 500m, Itens = new() { new ItemPedido { Descricao = "Item 1", Quantidade = 1, ValorUnitario = 500m } } };
        await connection.InsertManyGraphAsync(new[] { pedido });

        var insertedOrder = (await connection.QueryAsync<Pedido>("SELECT * FROM Pedidos WHERE NumeroDocumento = 'CASCADE-TEST' ")).First();
        var orderId = insertedOrder.Id;

        var children = (await connection.QueryAsync<ItemPedido>("SELECT * FROM ItensPedido WHERE PedidoId = @id", new { id = orderId })).ToList();
        await connection.DeleteManyAsync(children);

        var parent = (await connection.QueryAsync<Pedido>("SELECT * FROM Pedidos WHERE Id = @id", new { id = orderId })).ToList();
        var deleteCount = await connection.DeleteManyAsync(parent);

        Assert.Equal(1, deleteCount);

        var remainingParent = await connection.QueryAsync<Pedido>("SELECT * FROM Pedidos WHERE Id = @id", new { id = orderId });
        Assert.Empty(remainingParent);
    }
}
