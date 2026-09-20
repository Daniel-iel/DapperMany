using DapperMany.Samples.Models;
using Npgsql;

namespace DapperMany.Postgres.IntegrationTests;

/// <summary>
/// Integration tests for InsertManyGraph using PostgreSQL.
/// Uses IClassFixture&lt;PostgreSqlDatabaseFixture&gt; to share database configuration across test methods.
/// </summary>
public class InsertManyGraphIntegrationTests : IClassFixture<PostgreSqlDatabaseFixture>
{
    private readonly PostgreSqlDatabaseFixture _fixture;

    public InsertManyGraphIntegrationTests(PostgreSqlDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task InsertManyGraph_SingleParentWithChildren_PopulatesForeignKeys()
    {
        using var connection = new NpgsqlConnection(_fixture.ConnectionString);
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
        using var connection = new NpgsqlConnection(_fixture.ConnectionString);
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
        using var connection = new NpgsqlConnection(_fixture.ConnectionString);
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
        using var connection = new NpgsqlConnection(_fixture.ConnectionString);
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

