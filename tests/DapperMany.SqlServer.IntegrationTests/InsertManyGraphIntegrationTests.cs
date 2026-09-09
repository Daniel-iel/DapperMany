using System.Data;
using System.Diagnostics;
using DapperMany.Samples.Models;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace DapperMany.SqlServer.IntegrationTests;

/// <summary>
/// Integration tests for InsertManyGraph using SQL Server via Testcontainers.
/// Verifies that parent entities and their child collections are correctly inserted with FK auto-population.
/// </summary>
public class InsertManyGraphIntegrationTests : IAsyncLifetime
{
    private MsSqlContainer? _container;
    private string? _connectionString;

    public async Task InitializeAsync()
    {
        // Create SQL Server container
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2019-latest")
            .WithPassword("MyP@ssw0rd!!")
            .Build();

        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();

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

        // Create Pedidos table
        using var cmd1 = connection.CreateCommand();
        cmd1.CommandText = @"
            CREATE TABLE Pedidos (
                Id INT PRIMARY KEY IDENTITY(1,1),
                NumeroDocumento NVARCHAR(50) NOT NULL UNIQUE,
                DataPedido DATETIME2 NOT NULL,
                ValorTotal DECIMAL(12,2) NOT NULL,
                Status NVARCHAR(50) NOT NULL,
                Created DATETIME2 NOT NULL,
                Modified DATETIME2 NOT NULL
            )";
        await cmd1.ExecuteNonQueryAsync();

        // Create ItensPedido table
        using var cmd2 = connection.CreateCommand();
        cmd2.CommandText = @"
            CREATE TABLE ItensPedido (
                Id INT PRIMARY KEY IDENTITY(1,1),
                PedidoId INT NOT NULL,
                Descricao NVARCHAR(255) NOT NULL,
                Quantidade INT NOT NULL,
                ValorUnitario DECIMAL(12,2) NOT NULL,
                FOREIGN KEY (PedidoId) REFERENCES Pedidos(Id)
            )";
        await cmd2.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task InsertManyGraph_SingleParentWithChildren_PopulatesForeignKeys()
    {
        // Arrange
        using var connection = new SqlConnection(_connectionString);
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

        // Act
        var insertedCount = await connection.InsertManyGraphAsync(new[] { pedido });

        // Assert
        Assert.Equal(1, insertedCount); // One parent inserted

        // Verify parent was inserted
        using var selectParent = connection.CreateCommand();
        selectParent.CommandText = "SELECT COUNT(*) FROM Pedidos WHERE NumeroDocumento = 'PED-001'";
        var parentCount = (int?)await selectParent.ExecuteScalarAsync() ?? 0;
        Assert.Equal(1, parentCount);

        // Verify children were inserted with correct FK
        using var selectChildren = connection.CreateCommand();
        selectChildren.CommandText = "SELECT COUNT(*) FROM ItemPedidos WHERE PedidoId = (SELECT Id FROM Pedidos WHERE NumeroDocumento = 'PED-001')";
        var childCount = (int?)await selectChildren.ExecuteScalarAsync() ?? 0;
        Assert.Equal(2, childCount);
    }

    [Fact]
    public async Task InsertManyGraph_MultipleParentsWithVaryingChildCounts_AllInsertsSucceed()
    {
        // Arrange
        using var connection = new SqlConnection(_connectionString);
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

        // Act
        var insertedCount = await connection.InsertManyGraphAsync(pedidos);

        // Assert
        Assert.Equal(3, insertedCount);

        // Verify all parents inserted
        using var selectParents = connection.CreateCommand();
        selectParents.CommandText = "SELECT COUNT(*) FROM Pedidos WHERE NumeroDocumento IN ('PED-A', 'PED-B', 'PED-C')";
        var parentCount = (int?)await selectParents.ExecuteScalarAsync() ?? 0;
        Assert.Equal(3, parentCount);

        // Verify children counts
        using var selectChildrenA = connection.CreateCommand();
        selectChildrenA.CommandText = "SELECT COUNT(*) FROM ItemPedidos WHERE PedidoId = (SELECT Id FROM Pedidos WHERE NumeroDocumento = 'PED-A')";
        var childCountA = (int?)await selectChildrenA.ExecuteScalarAsync() ?? 0;
        Assert.Equal(1, childCountA);

        using var selectChildrenB = connection.CreateCommand();
        selectChildrenB.CommandText = "SELECT COUNT(*) FROM ItemPedidos WHERE PedidoId = (SELECT Id FROM Pedidos WHERE NumeroDocumento = 'PED-B')";
        var childCountB = (int?)await selectChildrenB.ExecuteScalarAsync() ?? 0;
        Assert.Equal(3, childCountB);

        using var selectChildrenC = connection.CreateCommand();
        selectChildrenC.CommandText = "SELECT COUNT(*) FROM ItemPedidos WHERE PedidoId = (SELECT Id FROM Pedidos WHERE NumeroDocumento = 'PED-C')";
        var childCountC = (int?)await selectChildrenC.ExecuteScalarAsync() ?? 0;
        Assert.Equal(0, childCountC);
    }

    [Fact]
    public async Task InsertManyGraph_ChildForeignKeyAutoPopulated_MatchesParentId()
    {
        // Arrange
        using var connection = new SqlConnection(_connectionString);
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

        // Act
        var insertedCount = await connection.InsertManyGraphAsync(new[] { pedido });

        // Assert
        Assert.Equal(1, insertedCount);

        // Verify FK was correctly set
        using var selectFK = connection.CreateCommand();
        selectFK.CommandText = @"
            SELECT i.PedidoId 
            FROM ItemPedidos i
            INNER JOIN Pedidos p ON i.PedidoId = p.Id
            WHERE p.NumeroDocumento = 'PED-FK-TEST'";
        var insertedFK = (int?)await selectFK.ExecuteScalarAsync() ?? 0;
        Assert.True(insertedFK > 0);

        // Verify the FK matches the parent ID
        using var selectParentId = connection.CreateCommand();
        selectParentId.CommandText = "SELECT Id FROM Pedidos WHERE NumeroDocumento = 'PED-FK-TEST'";
        var parentId = (int?)await selectParentId.ExecuteScalarAsync() ?? 0;
        Assert.Equal(parentId, insertedFK);
    }
}
