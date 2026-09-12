using DapperMany.Attributes;
using DapperMany.Internal.Mapping;
using Xunit;

namespace DapperMany.UnitTests.Mapping;

public class EntityMapperTests
{
    private class SimplePedido
    {
        [System.ComponentModel.DataAnnotations.Key]
        [System.ComponentModel.DataAnnotations.Schema.DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string? Cliente { get; set; }
        public DateTime Data { get; set; }
    }

    [System.ComponentModel.DataAnnotations.Schema.Table("Pedidos")]
    private class MappedPedido
    {
        [System.ComponentModel.DataAnnotations.Key]
        [System.ComponentModel.DataAnnotations.Schema.DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string? Cliente { get; set; }
    }

    [System.ComponentModel.DataAnnotations.Schema.Table("Pedidos")]
    private class PedidoWithGraph
    {
        [System.ComponentModel.DataAnnotations.Key]
        [System.ComponentModel.DataAnnotations.Schema.DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [HasMany(nameof(MappedItemPedido.PedidoId))]
        public List<MappedItemPedido>? Itens { get; set; }
    }

    [System.ComponentModel.DataAnnotations.Schema.Table("ItensPedido")]
    private class MappedItemPedido
    {
        [System.ComponentModel.DataAnnotations.Key]
        [System.ComponentModel.DataAnnotations.Schema.DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int PedidoId { get; set; }
        public string? Produto { get; set; }
    }

    [Fact]
    public void GetMetadata_MissingTableAttribute_ThrowsInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => EntityMapper.GetMetadata(typeof(SimplePedido)));
        Assert.Contains("[Table]", ex.Message);
    }

    [Fact]
    public void GetMetadata_MissingKeyProperty_ThrowsInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => EntityMapper.GetMetadata(typeof(PedidoNoKey)));
        Assert.Contains("[Key]", ex.Message);
    }

    [System.ComponentModel.DataAnnotations.Schema.Table("Pedidos")]
    private class PedidoNoKey
    {
        public string? Cliente { get; set; }
    }

    [Fact]
    public void GetMetadata_ValidEntity_ReturnsMetadata()
    {
        var metadata = EntityMapper.GetMetadata<MappedPedido>();

        Assert.NotNull(metadata);
        Assert.Equal(typeof(MappedPedido), metadata.EntityType);
        Assert.Equal("Pedidos", metadata.TableName);
        Assert.NotNull(metadata.KeyProperty);
        Assert.Equal(nameof(MappedPedido.Id), metadata.KeyProperty.Name);
    }

    [Fact]
    public void GetMetadata_WithIdentityProperty_IncludesInIdentityList()
    {
        var metadata = EntityMapper.GetMetadata<MappedPedido>();

        Assert.Single(metadata.IdentityProperties);
        Assert.Equal(nameof(MappedPedido.Id), metadata.IdentityProperties[0].Name);
    }

    [Fact]
    public void GetMetadata_WithHasMany_IncludesRelationship()
    {
        var metadata = EntityMapper.GetMetadata<PedidoWithGraph>();

        Assert.Single(metadata.Relationships);
        Assert.True(metadata.Relationships.ContainsKey(nameof(PedidoWithGraph.Itens)));

        var rel = metadata.Relationships[nameof(PedidoWithGraph.Itens)];
        Assert.Equal(typeof(MappedItemPedido), rel.ChildEntityType);
        Assert.Equal(nameof(MappedItemPedido.PedidoId), rel.ForeignKeyPropertyName);
    }

    [Fact]
    public void GetMetadata_CacheIsThreadSafe()
    {
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => EntityMapper.GetMetadata<MappedPedido>()))
            .ToArray();

        Task.WaitAll(tasks);

        var results = tasks.Select(t => t.Result).Distinct().Count();
        Assert.Equal(1, results); // All should return the same instance
    }

    [Fact]
    public void GetMetadata_MultipleTypes_IsolatedCaches()
    {
        var metadata1 = EntityMapper.GetMetadata<MappedPedido>();
        var metadata2 = EntityMapper.GetMetadata<MappedItemPedido>();

        Assert.NotEqual(metadata1.TableName, metadata2.TableName);
        Assert.Equal("Pedidos", metadata1.TableName);
        Assert.Equal("ItensPedido", metadata2.TableName);
    }
}
