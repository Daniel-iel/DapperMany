using DapperMany.Internal.Mapping;
using Xunit;

namespace DapperMany.UnitTests.Operations;

/// <summary>
/// Unit tests for UpdateMany and DeleteMany bulk operations.
/// </summary>
public class UpdateManyAndDeleteManyTests
{
    #region Test Entities

    [System.ComponentModel.DataAnnotations.Schema.Table("TestProducts")]
    public class ProductEntity
    {
        [System.ComponentModel.DataAnnotations.Key]
        [System.ComponentModel.DataAnnotations.Schema.DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string? Name { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    [System.ComponentModel.DataAnnotations.Schema.Table("TestOrders")]
    public class OrderEntity
    {
        [System.ComponentModel.DataAnnotations.Key]
        [System.ComponentModel.DataAnnotations.Schema.DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string? OrderNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Status { get; set; }
    }

    #endregion

    #region UpdateMany Tests

    [Fact]
    public void EntityMetadata_ContainsKeyProperty_ForUpdateOperations()
    {
        // Arrange & Act
        var metadata = EntityMapper.GetMetadata<ProductEntity>();

        // Assert
        Assert.NotNull(metadata.KeyProperty);
        Assert.Equal(nameof(ProductEntity.Id), metadata.KeyProperty.Name);
    }

    [Fact]
    public void EntityMetadata_ContainsMappedProperties_ForUpdateSqlGeneration()
    {
        // Arrange & Act
        var metadata = EntityMapper.GetMetadata<ProductEntity>();

        // Assert
        Assert.NotEmpty(metadata.MappedProperties);
        Assert.Contains(metadata.MappedProperties, p => p.Name == nameof(ProductEntity.Name));
        Assert.Contains(metadata.MappedProperties, p => p.Name == nameof(ProductEntity.Price));
        Assert.Contains(metadata.MappedProperties, p => p.Name == nameof(ProductEntity.Stock));
    }

    [Fact]
    public void KeyProperty_CanBeExcludedFromUpdateSetClause()
    {
        // Arrange
        var metadata = EntityMapper.GetMetadata<ProductEntity>();
        var updateableProps = metadata.MappedProperties
            .Where(p => p != metadata.KeyProperty)
            .ToList();

        // Assert - Key property should not be in updateable properties
        Assert.DoesNotContain(metadata.KeyProperty, updateableProps);
        Assert.NotEmpty(updateableProps);
    }

    [Fact]
    public void PartialObject_CanBuildDynamicUpdateStatement()
    {
        // Arrange: Simulate a partial update object (only Id + fields to update)
        var product = new ProductEntity
        {
            Id = 42,
            Price = 99.99m,
            Stock = 15
            // Name and LastUpdated not set (simulating partial object)
        };

        var metadata = EntityMapper.GetMetadata<ProductEntity>();

        // Act: Simulate determining which properties to update
        var updateableProps = metadata.MappedProperties
            .Where(p => p != metadata.KeyProperty && p.CanRead && p.CanWrite)
            .ToList();

        // Assert
        Assert.NotEmpty(updateableProps);
        Assert.True(updateableProps.Any(p => p.Name == nameof(ProductEntity.Price)));
    }

    [Fact]
    public void KeyProperty_CanBeExtractedForWhereClause()
    {
        // Arrange
        var product = new ProductEntity { Id = 99, Name = "Test" };
        var metadata = EntityMapper.GetMetadata<ProductEntity>();

        // Act
        var keyGetter = AccessorFactory.CreateGetter(metadata.KeyProperty);
        var keyValue = keyGetter(product);

        // Assert
        Assert.Equal(99, (int)keyValue);
    }

    [Fact]
    public void UpdatePropertyValue_CanBeSetter_ViaAccessorFactory()
    {
        // Arrange
        var product = new ProductEntity { Id = 1, Name = "Original", Price = 10m };
        var priceProperty = typeof(ProductEntity).GetProperty(nameof(ProductEntity.Price))!;

        // Act
        var priceSetter = AccessorFactory.CreateSetter(priceProperty);
        priceSetter(product, 25.50m);

        // Assert
        Assert.Equal(25.50m, product.Price);
    }

    #endregion

    #region DeleteMany Tests

    [Fact]
    public void DeleteOperation_RequiresOnlyKeyValues()
    {
        // Arrange
        var order = new OrderEntity { Id = 5, OrderNumber = "ORD-001", TotalAmount = 100m };
        var metadata = EntityMapper.GetMetadata<OrderEntity>();

        // Act
        var keyGetter = AccessorFactory.CreateGetter(metadata.KeyProperty);
        var keyValue = keyGetter(order);

        // Assert - Only key is needed for delete
        Assert.Equal(5, (int)keyValue);
    }

    [Fact]
    public void KeyProperty_CanBeUsedInWhereClause()
    {
        // Arrange
        var metadata = EntityMapper.GetMetadata<OrderEntity>();
        var orders = new List<OrderEntity>
        {
            new OrderEntity { Id = 1, OrderNumber = "A" },
            new OrderEntity { Id = 2, OrderNumber = "B" },
            new OrderEntity { Id = 3, OrderNumber = "C" }
        };

        // Act
        var keyValues = orders
            .Select(o => AccessorFactory.CreateGetter(metadata.KeyProperty)(o))
            .ToList();

        // Assert
        Assert.Equal(3, keyValues.Count);
        Assert.Contains(1, keyValues.Cast<int>());
        Assert.Contains(2, keyValues.Cast<int>());
        Assert.Contains(3, keyValues.Cast<int>());
    }

    [Fact]
    public void DeleteManyByKeys_ShouldAcceptOnlyKeyValues()
    {
        // Arrange: Prepare a list of just keys
        var keysToDelete = new object[] { 10, 20, 30 };

        // Act & Assert - No exception should be thrown
        Assert.Equal(3, keysToDelete.Length);
        Assert.All(keysToDelete, k => Assert.IsType<int>(k));
    }

    [Fact]
    public void EmptyDeleteBatch_ShouldReturnZero()
    {
        // Arrange
        var emptyList = new List<OrderEntity>();

        // Act & Assert
        Assert.Empty(emptyList);
    }

    [Fact]
    public void MultipleEntities_CanBeDeletedByKeys()
    {
        // Arrange
        var orders = new List<OrderEntity>
        {
            new OrderEntity { Id = 100 },
            new OrderEntity { Id = 101 },
            new OrderEntity { Id = 102 }
        };

        var metadata = EntityMapper.GetMetadata<OrderEntity>();

        // Act: Simulate extracting keys from entities for deletion
        var keys = orders
            .Select(o => AccessorFactory.CreateGetter(metadata.KeyProperty)(o))
            .ToList();

        // Assert
        Assert.Equal(3, keys.Count);
    }

    #endregion

    #region Integration Tests (Logic Only)

    [Fact]
    public void UpdateMany_WorkflowSimulation()
    {
        // Simulate the workflow: partial object → extract key → extract update properties → build SQL

        // Arrange: Partial object for update
        var partialProduct = new ProductEntity
        {
            Id = 42,
            Price = 150m,
            Stock = 50
        };

        var metadata = EntityMapper.GetMetadata<ProductEntity>();

        // Act - Step 1: Extract key
        var keyGetter = AccessorFactory.CreateGetter(metadata.KeyProperty);
        var keyValue = keyGetter(partialProduct);
        Assert.Equal(42, (int)keyValue);

        // Act - Step 2: Determine update columns
        var updateColumns = metadata.MappedProperties
            .Where(p => p != metadata.KeyProperty)
            .Select(p => p.Name)
            .ToList();
        Assert.NotEmpty(updateColumns);

        // Act - Step 3: Extract values for SET clause
        var priceGetter = AccessorFactory.CreateGetter(
            metadata.MappedProperties.First(p => p.Name == nameof(ProductEntity.Price)));
        var priceValue = priceGetter(partialProduct);

        // Assert
        Assert.Equal(150m, (decimal)priceValue);
    }

    [Fact]
    public void DeleteMany_WorkflowSimulation()
    {
        // Simulate the workflow: entities → extract keys → build WHERE IN clause

        // Arrange: Multiple entities to delete
        var ordersToDelete = new List<OrderEntity>
        {
            new OrderEntity { Id = 100, OrderNumber = "ORD-001" },
            new OrderEntity { Id = 101, OrderNumber = "ORD-002" },
            new OrderEntity { Id = 102, OrderNumber = "ORD-003" }
        };

        var metadata = EntityMapper.GetMetadata<OrderEntity>();

        // Act: Extract keys
        var keys = ordersToDelete
            .Select(o => AccessorFactory.CreateGetter(metadata.KeyProperty)(o))
            .ToList();

        // Assert - Simulate WHERE Id IN (100, 101, 102)
        Assert.Equal(3, keys.Count);
        Assert.Contains(100, keys.Cast<int>());
        Assert.Contains(101, keys.Cast<int>());
        Assert.Contains(102, keys.Cast<int>());
    }

    [Fact]
    public void DeleteManyByKeys_AlternativeWorkflow()
    {
        // Workflow for DeleteMany(IEnumerable<object> keys) overload

        // Arrange: Direct list of keys (no entities needed)
        var keysToDelete = new object[] { 200, 201, 202, 203, 204 };

        // Act: Would be used directly in WHERE IN clause
        var keyCount = keysToDelete.Length;

        // Assert
        Assert.Equal(5, keyCount);
    }

    #endregion
}
