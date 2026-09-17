using System.Data;
using System.Diagnostics;
using Dapper;
using DapperMany.Samples.Models;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace DapperMany.SqlServer.IntegrationTests;

/// <summary>
/// Integration tests for UpdateMany and DeleteMany operations using SQL Server.
/// </summary>
public class UpdateManyAndDeleteManyIntegrationTests : IAsyncLifetime
{
    private MsSqlContainer? _container;
    private string? _connectionString;

    public async Task InitializeAsync()
    {
        // Allow using an existing SQL Server (e.g. docker-compose) by setting TEST_SQLSERVER_CONNECTIONSTRING.
        var providedConnection = Environment.GetEnvironmentVariable("TEST_SQLSERVER_CONNECTIONSTRING");
        if (!string.IsNullOrWhiteSpace(providedConnection))
        {
            _connectionString = providedConnection;
        }
        else
        {
            // Create SQL Server container (password read from env var or fallback to docker-compose value)
            var saPassword = Environment.GetEnvironmentVariable("TEST_SQLSERVER_SA_PASSWORD") ?? "SqlServer123!";
            _container = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2019-latest")
                .WithPassword(saPassword)
                .Build();

            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();
        }

        // Ensure SQL Server provider is registered (module initializer may not run under test host).
        DapperMany.SqlServer.SqlServerProvider.Register();

        // Wait for SQL Server to accept connections (retry loop). Avoid flaky failures due to slow container startup.
        var ready = false;
        var sw = Stopwatch.StartNew();
        var timeout = TimeSpan.FromMinutes(5);
        while (sw.Elapsed < timeout)
        {
            try
            {
                using var testConn = new SqlConnection(_connectionString);
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
            throw new InvalidOperationException($"SQL Server container did not become ready within {timeout.TotalMinutes} minutes.");

        // Initialize schema
        await InitializeSchema();
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
            await _container.StopAsync();
    }

    private async Task InitializeSchema()
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Ensure database exists and switch to it (idempotent)
        using var useDb = connection.CreateCommand();
        useDb.CommandText = @"
            IF DB_ID('DapperMany') IS NULL
            BEGIN
                CREATE DATABASE [DapperMany];
            END
            USE [DapperMany];
        ";
        await useDb.ExecuteNonQueryAsync();

        // Create Pedidos table if it doesn't exist
        using var cmd1 = connection.CreateCommand();
        cmd1.CommandText = @"
            IF OBJECT_ID('dbo.Pedidos','U') IS NULL
            BEGIN
                CREATE TABLE dbo.Pedidos (
                    Id INT PRIMARY KEY IDENTITY(1,1),
                    NumeroDocumento NVARCHAR(50) NOT NULL UNIQUE,
                    DataPedido DATETIME2 NOT NULL,
                    ValorTotal DECIMAL(12,2) NOT NULL,
                    Status NVARCHAR(50) NOT NULL,
                    Created DATETIME2 NOT NULL,
                    Modified DATETIME2 NOT NULL
                );
            END
        ";
        await cmd1.ExecuteNonQueryAsync();

        // Create ItensPedido table if it doesn't exist
        using var cmd2 = connection.CreateCommand();
        cmd2.CommandText = @"
            IF OBJECT_ID('dbo.ItensPedido','U') IS NULL
            BEGIN
                CREATE TABLE dbo.ItensPedido (
                    Id INT PRIMARY KEY IDENTITY(1,1),
                    PedidoId INT NOT NULL,
                    Descricao NVARCHAR(255) NOT NULL,
                    Quantidade INT NOT NULL,
                    ValorUnitario DECIMAL(12,2) NOT NULL,
                    FOREIGN KEY (PedidoId) REFERENCES dbo.Pedidos(Id)
                );
            END
        ";
        await cmd2.ExecuteNonQueryAsync();

        // Clean up tables to ensure tests run idempotently (delete existing rows and reseed identities)
        using var cleanup = connection.CreateCommand();
        cleanup.CommandText = @"
            DELETE FROM dbo.ItensPedido;
            DELETE FROM dbo.Pedidos;
            DBCC CHECKIDENT('dbo.Pedidos', RESEED, 0);
            DBCC CHECKIDENT('dbo.ItensPedido', RESEED, 0);
        ";
        await cleanup.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task UpdateMany_UpdatesMultipleEntities()
    {
        // Arrange
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // First, insert some orders
        var pedidos = new List<Pedido>
        {
            new Pedido { NumeroDocumento = "UPD-001", ValorTotal = 100m, Status = "Pendente" },
            new Pedido { NumeroDocumento = "UPD-002", ValorTotal = 200m, Status = "Pendente" },
            new Pedido { NumeroDocumento = "UPD-003", ValorTotal = 300m, Status = "Pendente" }
        };

        var insertCount = await connection.InsertManyAsync(pedidos);
        Assert.Equal(3, insertCount);

        // Verify inserts
        var inserted = await connection.QueryAsync<Pedido>(
            "SELECT * FROM Pedidos WHERE NumeroDocumento LIKE 'UPD-%'");
        var pedidoList = inserted.ToList();
        Assert.Equal(3, pedidoList.Count);

        // Act - Update status to "Processado"
        var updatedPedidos = pedidoList.Select(p => new Pedido 
        { 
            Id = p.Id,
            NumeroDocumento = p.NumeroDocumento, // Required field
            Status = "Processado" 
        }).ToList();

        var updateCount = await connection.UpdateManyAsync(updatedPedidos);

        // Assert
        Assert.Equal(3, updateCount);

        // Verify updates
        var afterUpdate = await connection.QueryAsync<Pedido>(
            "SELECT * FROM Pedidos WHERE NumeroDocumento LIKE 'UPD-%' ORDER BY Id");
        var verifyList = afterUpdate.ToList();
        Assert.All(verifyList, p => Assert.Equal("Processado", p.Status));
    }

    [Fact]
    public async Task UpdateMany_PartialUpdate_OnlyModifiesSpecifiedColumns()
    {
        // Arrange
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var pedido = new Pedido 
        { 
            NumeroDocumento = "PARTIAL-UPDATE", 
            ValorTotal = 500m, 
            Status = "Pendente" 
        };

        var insertCount = await connection.InsertManyAsync(new[] { pedido });
        Assert.Equal(1, insertCount);

        // Get the inserted ID
        var inserted = await connection.QueryAsync<Pedido>(
            "SELECT * FROM Pedidos WHERE NumeroDocumento = 'PARTIAL-UPDATE'");
        var insertedPedido = inserted.First();

        // Act - Partial update: only update Status, leave ValorTotal unchanged
        var partialUpdate = new Pedido 
        { 
            Id = insertedPedido.Id,
            NumeroDocumento = insertedPedido.NumeroDocumento, // Required field
            Status = "Cancelado" 
            // Note: ValorTotal not set - should remain 500m
        };

        var updateCount = await connection.UpdateManyAsync(new[] { partialUpdate });

        // Assert
        Assert.Equal(1, updateCount);

        // Verify
        var afterUpdate = await connection.QueryAsync<Pedido>(
            "SELECT * FROM Pedidos WHERE Id = @id", new { id = insertedPedido.Id });
        var updated = afterUpdate.First();
        Assert.Equal("Cancelado", updated.Status);
        Assert.Equal(500m, updated.ValorTotal); // Should remain unchanged
    }

    [Fact]
    public async Task DeleteMany_DeletesMultipleEntities()
    {
        // Arrange
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Insert test orders
        var pedidos = new List<Pedido>
        {
            new Pedido { NumeroDocumento = "DEL-001", ValorTotal = 100m },
            new Pedido { NumeroDocumento = "DEL-002", ValorTotal = 200m },
            new Pedido { NumeroDocumento = "DEL-003", ValorTotal = 300m }
        };

        await connection.InsertManyAsync(pedidos);

        // Get inserted IDs
        var inserted = await connection.QueryAsync<Pedido>(
            "SELECT * FROM Pedidos WHERE NumeroDocumento LIKE 'DEL-%'");
        var pedidosToDelete = inserted.ToList();
        Assert.Equal(3, pedidosToDelete.Count);

        // Act - Delete by entities
        var deleteCount = await connection.DeleteManyAsync(pedidosToDelete);

        // Assert
        Assert.Equal(3, deleteCount);

        // Verify deletion
        var remaining = await connection.QueryAsync<Pedido>(
            "SELECT * FROM Pedidos WHERE NumeroDocumento LIKE 'DEL-%'");
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task DeleteManyByKeys_DeletesEntitiesByKeyValuesOnly()
    {
        // Arrange
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Insert test orders
        var pedidos = new List<Pedido>
        {
            new Pedido { NumeroDocumento = "DELKEY-001", ValorTotal = 100m },
            new Pedido { NumeroDocumento = "DELKEY-002", ValorTotal = 200m },
            new Pedido { NumeroDocumento = "DELKEY-003", ValorTotal = 300m }
        };

        await connection.InsertManyAsync(pedidos);

        // Get inserted IDs
        var inserted = await connection.QueryAsync<Pedido>(
            "SELECT * FROM Pedidos WHERE NumeroDocumento LIKE 'DELKEY-%'");
        var pedidoIds = inserted.Select(p => (object)p.Id).ToList();
        Assert.Equal(3, pedidoIds.Count);

        // Act - Delete by keys only (no entities needed)
        var deleteCount = await connection.DeleteManyAsync<Pedido>(pedidoIds);

        // Assert
        Assert.Equal(3, deleteCount);

        // Verify deletion
        var remaining = await connection.QueryAsync<Pedido>(
            "SELECT * FROM Pedidos WHERE NumeroDocumento LIKE 'DELKEY-%'");
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task UpdateMany_WithChildEntities_DoesNotAffectChildren()
    {
        // Arrange
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Insert order with items
        var pedido = new Pedido
        {
            NumeroDocumento = "CHILD-TEST",
            ValorTotal = 1000m,
            Status = "Pendente",
            Itens = new()
            {
                new ItemPedido { Descricao = "Item 1", Quantidade = 2, ValorUnitario = 500m }
            }
        };

        await connection.InsertManyAsync(new[] { pedido });

        // Get inserted order ID
        var inserted = await connection.QueryAsync<Pedido>(
            "SELECT * FROM Pedidos WHERE NumeroDocumento = 'CHILD-TEST'");
        var insertedPedido = inserted.First();

        // Act - Update only the parent order status
        var update = new Pedido 
        { 
            Id = insertedPedido.Id,
            NumeroDocumento = insertedPedido.NumeroDocumento, // Required field
            Status = "Processado" 
        };

        var updateCount = await connection.UpdateManyAsync(new[] { update });

        // Assert
        Assert.Equal(1, updateCount);

        // Verify parent updated
        var afterUpdate = await connection.QueryAsync<Pedido>(
            "SELECT * FROM Pedidos WHERE Id = @id", new { id = insertedPedido.Id });
        Assert.Equal("Processado", afterUpdate.First().Status);

        // Verify children untouched
        var children = await connection.QueryAsync<ItemPedido>(
            "SELECT * FROM ItensPedido WHERE PedidoId = @id", new { id = insertedPedido.Id });
        Assert.Single(children);
    }

    [Fact]
    public async Task DeleteMany_CascadeToChildren_MustBeExplicitForV1()
    {
        // Arrange
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Insert order with items
        var pedido = new Pedido
        {
            NumeroDocumento = "CASCADE-TEST",
            ValorTotal = 500m,
            Itens = new()
            {
                new ItemPedido { Descricao = "Item 1", Quantidade = 1, ValorUnitario = 500m }
            }
        };

        await connection.InsertManyAsync(new[] { pedido });

        // Get IDs
        var insertedOrder = await connection.QueryAsync<Pedido>(
            "SELECT * FROM Pedidos WHERE NumeroDocumento = 'CASCADE-TEST'");
        var orderId = insertedOrder.First().Id;

        // Act - Delete children first (required for v1)
        var children = await connection.QueryAsync<ItemPedido>(
            "SELECT * FROM ItensPedido WHERE PedidoId = @id", new { id = orderId });
        await connection.DeleteManyAsync(children);

        // Then delete parent
        var parent = await connection.QueryAsync<Pedido>(
            "SELECT * FROM Pedidos WHERE Id = @id", new { id = orderId });
        var deleteCount = await connection.DeleteManyAsync(parent);

        // Assert - Cascade not automatic in v1
        Assert.Equal(1, deleteCount);

        // Verify both deleted
        var remainingParent = await connection.QueryAsync<Pedido>(
            "SELECT * FROM Pedidos WHERE Id = @id", new { id = orderId });
        Assert.Empty(remainingParent);
    }
}
