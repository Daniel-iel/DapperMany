using System.Diagnostics;
using Dapper;
using DapperMany.Samples.Models;
using MySqlConnector;
using Testcontainers.MySql;

namespace DapperMany.MySql.IntegrationTests;

public class InsertManyGraphIntegrationTests : IAsyncLifetime
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
                ValorTotal DECIMAL(18,2) NOT NULL,
                Created DATETIME NOT NULL,
                Modified DATETIME NOT NULL,
                CONSTRAINT FK_ItensPedido_Pedidos FOREIGN KEY (PedidoId) REFERENCES Pedidos(Id) ON DELETE CASCADE
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

        // Ensure ValorTotal column exists on ItensPedido for older DBs
        using var alter = connection.CreateCommand();
        alter.CommandText = @"ALTER TABLE ItensPedido ADD COLUMN IF NOT EXISTS ValorTotal DECIMAL(18,2) NOT NULL DEFAULT 0;";
        try
        {
            await alter.ExecuteNonQueryAsync();
        }
        catch
        {
            // Ignore if MySQL version doesn't support IF NOT EXISTS for ADD COLUMN
        }
        var alterCreated = connection.CreateCommand();
        alterCreated.CommandText = @"ALTER TABLE ItensPedido ADD COLUMN IF NOT EXISTS Created DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP;";
        try { await alterCreated.ExecuteNonQueryAsync(); } catch { }
        var alterModified = connection.CreateCommand();
        alterModified.CommandText = @"ALTER TABLE ItensPedido ADD COLUMN IF NOT EXISTS Modified DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP;";
        try { await alterModified.ExecuteNonQueryAsync(); } catch { }
    }

    [Fact]
    public async Task InsertManyGraph_SingleParentWithChildren_PopulatesForeignKeys()
    {
        using var connection = new MySqlConnection(_connectionString);
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

        var parentCount = Convert.ToInt32(await connection.ExecuteScalarAsync("SELECT COUNT(*) FROM Pedidos WHERE NumeroDocumento = 'PED-001'"));
        Assert.Equal(1, parentCount);

        var childCount = Convert.ToInt32(await connection.ExecuteScalarAsync("SELECT COUNT(*) FROM ItensPedido WHERE PedidoId = (SELECT Id FROM Pedidos WHERE NumeroDocumento = 'PED-001')"));
        Assert.Equal(2, childCount);
    }

    [Fact]
    public async Task InsertManyGraph_MultipleParentsWithVaryingChildCounts_AllInsertsSucceed()
    {
        using var connection = new MySqlConnection(_connectionString);
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

        var parentCount = Convert.ToInt32(await connection.ExecuteScalarAsync("SELECT COUNT(*) FROM Pedidos WHERE NumeroDocumento IN ('PED-A','PED-B','PED-C')"));
        Assert.Equal(3, parentCount);

        var childCountA = Convert.ToInt32(await connection.ExecuteScalarAsync("SELECT COUNT(*) FROM ItensPedido WHERE PedidoId = (SELECT Id FROM Pedidos WHERE NumeroDocumento = 'PED-A')"));
        Assert.Equal(1, childCountA);

        var childCountB = Convert.ToInt32(await connection.ExecuteScalarAsync("SELECT COUNT(*) FROM ItensPedido WHERE PedidoId = (SELECT Id FROM Pedidos WHERE NumeroDocumento = 'PED-B')"));
        Assert.Equal(3, childCountB);

        var childCountC = Convert.ToInt32(await connection.ExecuteScalarAsync("SELECT COUNT(*) FROM ItensPedido WHERE PedidoId = (SELECT Id FROM Pedidos WHERE NumeroDocumento = 'PED-C')"));
        Assert.Equal(0, childCountC);
    }

    [Fact]
    public async Task InsertManyGraph_ChildForeignKeyAutoPopulated_MatchesParentId()
    {
        using var connection = new MySqlConnection(_connectionString);
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

        var insertedFK = Convert.ToInt32(await connection.ExecuteScalarAsync("SELECT i.PedidoId FROM ItensPedido i INNER JOIN Pedidos p ON i.PedidoId = p.Id WHERE p.NumeroDocumento = 'PED-FK-TEST'"));
        Assert.True(insertedFK > 0);

        var parentId = Convert.ToInt32(await connection.ExecuteScalarAsync("SELECT Id FROM Pedidos WHERE NumeroDocumento = 'PED-FK-TEST'"));
        Assert.Equal(parentId, insertedFK);
    }

    [Fact]
    public async Task InsertManyGraph_NullChildren_InsertsParentOnly()
    {
        using var connection = new MySqlConnection(_connectionString);
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

        var parentCount = Convert.ToInt32(await connection.ExecuteScalarAsync("SELECT COUNT(*) FROM Pedidos WHERE NumeroDocumento = 'PED-NULL'"));
        Assert.Equal(1, parentCount);

        var childCount = Convert.ToInt32(await connection.ExecuteScalarAsync("SELECT COUNT(*) FROM ItensPedido WHERE PedidoId = (SELECT Id FROM Pedidos WHERE NumeroDocumento = 'PED-NULL')"));
        Assert.Equal(0, childCount);
    }
}
