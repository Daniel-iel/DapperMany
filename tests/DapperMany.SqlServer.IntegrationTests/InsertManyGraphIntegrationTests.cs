using DapperMany.Samples.Models;
using Microsoft.Data.SqlClient;

namespace DapperMany.SqlServer.IntegrationTests;

/// <summary>
/// Integration tests for InsertManyGraph using SQL Server.
/// Uses IClassFixture&lt;SqlServerDatabaseFixture&gt; to share database configuration across test methods.
/// </summary>
public class InsertManyGraphIntegrationTests : IClassFixture<SqlServerDatabaseFixture>
{
    private readonly SqlServerDatabaseFixture _fixture;

    public InsertManyGraphIntegrationTests(SqlServerDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task InsertManyGraph_SingleParentWithChildren_PopulatesForeignKeys()
    {
        // Arrange
        using var connection = new SqlConnection(_fixture.ConnectionString);
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
        var insertedCount = await connection.InsertManyAsync(new[] { pedido });

        // Assert
        Assert.Equal(1, insertedCount); // One parent inserted

        // Verify parent was inserted
        using var selectParent = connection.CreateCommand();
        selectParent.CommandText = "SELECT COUNT(*) FROM Pedidos WHERE NumeroDocumento = 'PED-001'";
        var parentCount = (int?)await selectParent.ExecuteScalarAsync() ?? 0;
        Assert.Equal(1, parentCount);

        // Verify children were inserted with correct FK
        using var selectChildren = connection.CreateCommand();
        selectChildren.CommandText = "SELECT COUNT(*) FROM ItensPedido WHERE PedidoId = (SELECT Id FROM Pedidos WHERE NumeroDocumento = 'PED-001')";
        var childCount = (int?)await selectChildren.ExecuteScalarAsync() ?? 0;
        Assert.Equal(2, childCount);
    }

    [Fact]
    public async Task InsertManyGraph_MultipleParentsWithVaryingChildCounts_AllInsertsSucceed()
    {
        // Arrange
        using var connection = new SqlConnection(_fixture.ConnectionString);
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
        var insertedCount = await connection.InsertManyAsync(pedidos);

        // Assert
        Assert.Equal(3, insertedCount);

        // Verify all parents inserted
        using var selectParents = connection.CreateCommand();
        selectParents.CommandText = "SELECT COUNT(*) FROM Pedidos WHERE NumeroDocumento IN ('PED-A', 'PED-B', 'PED-C')";
        var parentCount = (int?)await selectParents.ExecuteScalarAsync() ?? 0;
        Assert.Equal(3, parentCount);

        // Verify children counts
        using var selectChildrenA = connection.CreateCommand();
        selectChildrenA.CommandText = "SELECT COUNT(*) FROM ItensPedido WHERE PedidoId = (SELECT Id FROM Pedidos WHERE NumeroDocumento = 'PED-A')";
        var childCountA = (int?)await selectChildrenA.ExecuteScalarAsync() ?? 0;
        Assert.Equal(1, childCountA);

        using var selectChildrenB = connection.CreateCommand();
        selectChildrenB.CommandText = "SELECT COUNT(*) FROM ItensPedido WHERE PedidoId = (SELECT Id FROM Pedidos WHERE NumeroDocumento = 'PED-B')";
        var childCountB = (int?)await selectChildrenB.ExecuteScalarAsync() ?? 0;
        Assert.Equal(3, childCountB);

        using var selectChildrenC = connection.CreateCommand();
        selectChildrenC.CommandText = "SELECT COUNT(*) FROM ItensPedido WHERE PedidoId = (SELECT Id FROM Pedidos WHERE NumeroDocumento = 'PED-C')";
        var childCountC = (int?)await selectChildrenC.ExecuteScalarAsync() ?? 0;
        Assert.Equal(0, childCountC);
    }

    [Fact]
    public async Task InsertManyGraph_ChildForeignKeyAutoPopulated_MatchesParentId()
    {
        // Arrange
        using var connection = new SqlConnection(_fixture.ConnectionString);
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
        var insertedCount = await connection.InsertManyAsync(new[] { pedido });

        // Assert
        Assert.Equal(1, insertedCount);

        // Verify FK was correctly set
        using var selectFK = connection.CreateCommand();
        selectFK.CommandText = @"
            SELECT i.PedidoId 
            FROM ItensPedido i
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

    [Fact]
    public async Task InsertManyGraph_NullChildren_InsertsParentOnly()
    {
        // Arrange
        using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        var pedido = new Pedido
        {
            NumeroDocumento = "PED-NULL",
            DataPedido = DateTime.UtcNow,
            ValorTotal = 100m,
            Status = "Pendente",
            Itens = null
        };

        // Act
        var insertedCount = await connection.InsertManyAsync(new[] { pedido });

        // Assert
        Assert.Equal(1, insertedCount);

        using var selectParent = connection.CreateCommand();
        selectParent.CommandText = "SELECT COUNT(*) FROM Pedidos WHERE NumeroDocumento = 'PED-NULL'";
        var parentCount = (int?)await selectParent.ExecuteScalarAsync() ?? 0;
        Assert.Equal(1, parentCount);

        using var selectChildren = connection.CreateCommand();
        selectChildren.CommandText = "SELECT COUNT(*) FROM ItensPedido WHERE PedidoId = (SELECT Id FROM Pedidos WHERE NumeroDocumento = 'PED-NULL')";
        var childCount = (int?)await selectChildren.ExecuteScalarAsync() ?? 0;
        Assert.Equal(0, childCount);
    }
}

