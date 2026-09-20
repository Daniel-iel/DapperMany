using Dapper;
using DapperMany.Samples.Models;
using Microsoft.Data.SqlClient;
using Xunit;

namespace DapperMany.SqlServer.IntegrationTests;

/// <summary>
/// Integration tests for composite key operations (InsertMany, UpdateMany, DeleteMany)
/// using SQL Server via centralized SqlServerDatabaseFixture.
/// Tests composite key patterns for multi-tenant scenarios with primary keys on multiple columns.
/// </summary>
public class CompositeKeyIntegrationTests : IClassFixture<SqlServerDatabaseFixture>
{
    private readonly SqlServerDatabaseFixture _fixture;

    public CompositeKeyIntegrationTests(SqlServerDatabaseFixture fixture)
    {
        _fixture = fixture;
        // Initialize composite key schema when fixture is created
        _fixture.InitializeCompositeKeySchema().Wait();
    }

    [Fact]
    public async Task InsertManyWithCompositeKey_Should_Insert_Multiple_Records()
    {
        using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        var orders = new[]
        {
            new TenantPedido { TenantId = "TENANT_A", DocumentNumber = "DOC001", TotalAmount = 100.00m, Status = "Pendente" },
            new TenantPedido { TenantId = "TENANT_A", DocumentNumber = "DOC002", TotalAmount = 200.00m, Status = "Pendente" },
            new TenantPedido { TenantId = "TENANT_B", DocumentNumber = "DOC001", TotalAmount = 150.00m, Status = "Pendente" }
        };

        var inserted = await connection.InsertManyAsync(orders);
        
        Assert.Equal(3, inserted);

        // Verify records were inserted
        var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM TenantPedidos");
        Assert.Equal(3, count);

        // Verify composite key uniqueness - both parts matter
        var tenantADoc001 = await connection.QuerySingleOrDefaultAsync<TenantPedido>(
            "SELECT * FROM TenantPedidos WHERE TenantId = @TenantId AND DocumentNumber = @DocumentNumber",
            new { TenantId = "TENANT_A", DocumentNumber = "DOC001" });
        Assert.NotNull(tenantADoc001);
        Assert.Equal(100.00m, tenantADoc001.TotalAmount);
    }

    [Fact]
    public async Task UpdateManyWithCompositeKey_Should_Update_By_Composite_Key()
    {
        using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        // Insert test data
        var orders = new[]
        {
            new TenantPedido { TenantId = "TENANT_A", DocumentNumber = "DOC001", TotalAmount = 100.00m, Status = "Pendente" },
            new TenantPedido { TenantId = "TENANT_A", DocumentNumber = "DOC002", TotalAmount = 200.00m, Status = "Pendente" },
            new TenantPedido { TenantId = "TENANT_B", DocumentNumber = "DOC001", TotalAmount = 150.00m, Status = "Pendente" }
        };

        await connection.InsertManyAsync(orders);

        // Update records
        orders[0].Status = "Processado";
        orders[0].TotalAmount = 110.00m;
        orders[2].Status = "Cancelado";

        var updated = await connection.UpdateManyAsync(new[] { orders[0], orders[2] });
        Assert.Equal(2, updated);

        // Verify updates
        var updatedOrder = await connection.QuerySingleOrDefaultAsync<TenantPedido>(
            "SELECT * FROM TenantPedidos WHERE TenantId = @TenantId AND DocumentNumber = @DocumentNumber",
            new { TenantId = "TENANT_A", DocumentNumber = "DOC001" });
        Assert.NotNull(updatedOrder);
        Assert.Equal("Processado", updatedOrder.Status);
        Assert.Equal(110.00m, updatedOrder.TotalAmount);

        var otherUpdate = await connection.QuerySingleOrDefaultAsync<TenantPedido>(
            "SELECT * FROM TenantPedidos WHERE TenantId = @TenantId AND DocumentNumber = @DocumentNumber",
            new { TenantId = "TENANT_B", DocumentNumber = "DOC001" });
        Assert.NotNull(otherUpdate);
        Assert.Equal("Cancelado", otherUpdate.Status);
    }

    [Fact]
    public async Task DeleteManyWithCompositeKey_Should_Delete_By_Composite_Key()
    {
        using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        // Insert test data
        var orders = new[]
        {
            new TenantPedido { TenantId = "TENANT_A", DocumentNumber = "DOC001", TotalAmount = 100.00m, Status = "Pendente" },
            new TenantPedido { TenantId = "TENANT_A", DocumentNumber = "DOC002", TotalAmount = 200.00m, Status = "Pendente" },
            new TenantPedido { TenantId = "TENANT_B", DocumentNumber = "DOC001", TotalAmount = 150.00m, Status = "Pendente" }
        };

        await connection.InsertManyAsync(orders);

        // Delete records
        var deleted = await connection.DeleteManyAsync(new[] { orders[0], orders[2] });
        Assert.Equal(2, deleted);

        // Verify deletions
        var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM TenantPedidos");
        Assert.Equal(1, count);

        var remaining = await connection.QuerySingleOrDefaultAsync<TenantPedido>(
            "SELECT * FROM TenantPedidos");
        Assert.NotNull(remaining);
        Assert.Equal("TENANT_A", remaining.TenantId);
        Assert.Equal("DOC002", remaining.DocumentNumber);
    }



    [Fact]
    public async Task CompositeKey_Should_Support_Large_Batch_Operations()
    {
        using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        // Create 100+ records with composite keys
        var orders = Enumerable.Range(0, 120)
            .Select(i => new TenantPedido
            {
                TenantId = $"TENANT_{i % 10}",
                DocumentNumber = $"DOC{i:D3}",
                TotalAmount = 100m + i,
                Status = "Pendente"
            })
            .ToList();

        var inserted = await connection.InsertManyAsync(orders);
        Assert.Equal(120, inserted);

        // Update subset
        var toUpdate = orders.Where(o => o.TenantId == "TENANT_0").Take(10).ToList();
        foreach (var order in toUpdate)
            order.Status = "Processado";

        var updated = await connection.UpdateManyAsync(toUpdate);
        Assert.True(updated > 0);

        // Delete subset
        var toDelete = orders.Where(o => o.TenantId == "TENANT_1").Take(5).ToList();
        var deleted = await connection.DeleteManyAsync(toDelete);
        Assert.Equal(5, deleted);

        var finalCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM TenantPedidos");
        Assert.Equal(115, finalCount); // 120 - 5 deleted
    }
}
