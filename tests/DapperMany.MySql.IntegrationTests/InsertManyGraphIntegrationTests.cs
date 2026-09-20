using Dapper;
using DapperMany.Samples.Models;
using MySqlConnector;

namespace DapperMany.MySql.IntegrationTests;

public class InsertManyGraphIntegrationTests : IClassFixture<MySqlDatabaseFixture>
{
    private readonly MySqlDatabaseFixture _fixture;

    public InsertManyGraphIntegrationTests(MySqlDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task InsertManyGraph_SingleParentWithChildren_PopulatesForeignKeys()
    {
        using var connection = new MySqlConnection(_fixture.ConnectionString);
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

        var insertedCount = await connection.InsertManyAsync(new[] { pedido });
        Assert.Equal(1, insertedCount);

        var parentCount = Convert.ToInt32(await connection.ExecuteScalarAsync("SELECT COUNT(*) FROM Pedidos WHERE NumeroDocumento = 'PED-001'"));
        Assert.Equal(1, parentCount);

        var childCount = Convert.ToInt32(await connection.ExecuteScalarAsync("SELECT COUNT(*) FROM ItensPedido WHERE PedidoId = (SELECT Id FROM Pedidos WHERE NumeroDocumento = 'PED-001')"));
        Assert.Equal(2, childCount);
    }

    [Fact]
    public async Task InsertManyGraph_MultipleParentsWithVaryingChildCounts_AllInsertsSucceed()
    {
        using var connection = new MySqlConnection(_fixture.ConnectionString);
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

        var insertedCount = await connection.InsertManyAsync(pedidos);

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
        using var connection = new MySqlConnection(_fixture.ConnectionString);
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

        var insertedCount = await connection.InsertManyAsync(new[] { pedido });
        Assert.Equal(1, insertedCount);

        var insertedFK = Convert.ToInt32(await connection.ExecuteScalarAsync("SELECT i.PedidoId FROM ItensPedido i INNER JOIN Pedidos p ON i.PedidoId = p.Id WHERE p.NumeroDocumento = 'PED-FK-TEST'"));
        Assert.True(insertedFK > 0);

        var parentId = Convert.ToInt32(await connection.ExecuteScalarAsync("SELECT Id FROM Pedidos WHERE NumeroDocumento = 'PED-FK-TEST'"));
        Assert.Equal(parentId, insertedFK);
    }

    [Fact]
    public async Task InsertManyGraph_NullChildren_InsertsParentOnly()
    {
        using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        var pedido = new Pedido
        {
            NumeroDocumento = "PED-NULL",
            DataPedido = DateTime.UtcNow,
            ValorTotal = 100m,
            Status = "Pendente",
            Itens = null
        };

        var insertedCount = await connection.InsertManyAsync(new[] { pedido });
        Assert.Equal(1, insertedCount);

        var parentCount = Convert.ToInt32(await connection.ExecuteScalarAsync("SELECT COUNT(*) FROM Pedidos WHERE NumeroDocumento = 'PED-NULL'"));
        Assert.Equal(1, parentCount);

        var childCount = Convert.ToInt32(await connection.ExecuteScalarAsync("SELECT COUNT(*) FROM ItensPedido WHERE PedidoId = (SELECT Id FROM Pedidos WHERE NumeroDocumento = 'PED-NULL')"));
        Assert.Equal(0, childCount);
    }
}
