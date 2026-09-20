using System.Reflection;
using DapperMany.Attributes;
using DapperMany.Internal.Mapping;
using Xunit;

namespace DapperMany.UnitTests.Graph;

/// <summary>
/// Unit tests for GraphInsertOrchestrator covering FK population, 
/// relationship handling, and parent-child orchestration.
/// 
/// Focus: These tests verify the infrastructure needed for graph inserts:
/// - Attribute detection ([HasMany])
/// - Property reflection
/// - FK property resolution and setting
/// - Collection element type extraction
/// - FK population workflow with AccessorFactory
/// 
/// Note: EntityMapper.BuildMetadata() relationship detection is being debugged separately.
/// For now, tests focus on verifying individual components work correctly.
/// </summary>
public class GraphInsertOrchestratorTests
{
    #region Test Entities

    [System.ComponentModel.DataAnnotations.Schema.Table("Orders")]
    public class OrderEntity
    {
        [System.ComponentModel.DataAnnotations.Key]
        [System.ComponentModel.DataAnnotations.Schema.DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string? OrderNumber { get; set; }

        [HasMany(nameof(OrderItemEntity.OrderId))]
        public List<OrderItemEntity> Items { get; set; } = new();
    }

    [System.ComponentModel.DataAnnotations.Schema.Table("OrderItems")]
    public class OrderItemEntity
    {
        [System.ComponentModel.DataAnnotations.Key]
        [System.ComponentModel.DataAnnotations.Schema.DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public string? ProductName { get; set; }
    }

    #endregion

    #region Attribute Detection Tests

    [Fact]
    public void OrderEntity_HasManyAttribute_IsDetectable()
    {
        // Arrange
        var itemsProp = typeof(OrderEntity).GetProperty(nameof(OrderEntity.Items));

        // Act
        var hasMany = itemsProp?.GetCustomAttribute<HasManyAttribute>();

        // Assert
        Assert.NotNull(itemsProp);
        Assert.NotNull(hasMany);
        Assert.Equal(nameof(OrderItemEntity.OrderId), hasMany.ForeignKey);
    }

    [Fact]
    public void OrderItemEntity_NoHasMany_AttributeOnLeafEntity()
    {
        // Arrange
        var props = typeof(OrderItemEntity).GetProperties();

        // Act
        var hasAnyHasMany = props.Any(p => p.GetCustomAttribute<HasManyAttribute>() != null);

        // Assert - leaf entity should have no [HasMany] attributes
        Assert.False(hasAnyHasMany);
    }

    #endregion

    #region Property Reflection Tests

    [Fact]
    public void OrderEntity_Items_PropertyIsFound_ViaReflection()
    {
        // Arrange & Act
        var allProps = typeof(OrderEntity).GetProperties();
        var itemsProp = allProps.FirstOrDefault(p => p.Name == nameof(OrderEntity.Items));

        // Assert
        Assert.NotNull(itemsProp);
        Assert.Equal(typeof(List<OrderItemEntity>), itemsProp.PropertyType);
    }

    [Fact]
    public void OrderItemEntity_OrderId_PropertyIsFound_ViaReflection()
    {
        // Arrange & Act
        var allProps = typeof(OrderItemEntity).GetProperties();
        var orderIdProp = allProps.FirstOrDefault(p => p.Name == nameof(OrderItemEntity.OrderId));

        // Assert
        Assert.NotNull(orderIdProp);
        Assert.Equal(typeof(int), orderIdProp.PropertyType);
    }

    #endregion

    #region FK Property Resolution Tests

    [Fact]
    public void ForeignKeyProperty_CanBeResolved_ByName()
    {
        // Arrange
        var fkPropertyName = nameof(OrderItemEntity.OrderId);
        var childType = typeof(OrderItemEntity);

        // Act
        var fkProperty = childType.GetProperty(fkPropertyName);

        // Assert
        Assert.NotNull(fkProperty);
        Assert.Equal(typeof(int), fkProperty.PropertyType);
    }

    [Fact]
    public void ForeignKeyProperty_CanBeSetter_ForPopulation()
    {
        // Arrange
        var fkProperty = typeof(OrderItemEntity).GetProperty(nameof(OrderItemEntity.OrderId));
        var item = new OrderItemEntity { ProductName = "Widget" };

        // Act
        var fkSetter = AccessorFactory.CreateSetter(fkProperty!);
        fkSetter(item, 99);

        // Assert
        Assert.Equal(99, item.OrderId);
    }

    [Fact]
    public void ForeignKeyProperty_CanBeGetter_ForReading()
    {
        // Arrange
        var fkProperty = typeof(OrderItemEntity).GetProperty(nameof(OrderItemEntity.OrderId));
        var item = new OrderItemEntity { OrderId = 42, ProductName = "Widget" };

        // Act
        var fkGetter = AccessorFactory.CreateGetter(fkProperty!);
        var fkValue = fkGetter(item);

        // Assert
        Assert.Equal(42, (int)fkValue);
    }

    #endregion

    #region Collection Type Extraction Tests

    [Fact]
    public void CollectionElementType_CanBeExtracted_FromListOfT()
    {
        // Arrange
        var itemsProp = typeof(OrderEntity).GetProperty(nameof(OrderEntity.Items));
        var collectionType = itemsProp!.PropertyType;

        // Act
        Type? elementType = null;
        if (collectionType.IsGenericType)
        {
            var genArgs = collectionType.GetGenericArguments();
            elementType = genArgs.Length > 0 ? genArgs[0] : null;
        }

        // Assert
        Assert.NotNull(elementType);
        Assert.Equal(typeof(OrderItemEntity), elementType);
    }

    #endregion

    #region EntityMapper Metadata Tests

    [Fact]
    public void EntityMetadata_IsInitializedWithEmptyRelationships_WhenNoHasMany()
    {
        // Arrange & Act
        var metadata = EntityMapper.GetMetadata<OrderItemEntity>();

        // Assert - leaf entity should have empty relationships dict, not null
        Assert.NotNull(metadata);
        Assert.NotNull(metadata.Relationships);
        Assert.Empty(metadata.Relationships);
    }

    [Fact]
    public void ChildEntityMetadata_CanBeRetrieved_ByType()
    {
        // Arrange & Act
        var childMetadata = EntityMapper.GetMetadata<OrderItemEntity>();

        // Assert
        Assert.NotNull(childMetadata);
        Assert.Equal(typeof(OrderItemEntity), childMetadata.EntityType);
        Assert.Equal("OrderItems", childMetadata.TableName);
        Assert.Equal(nameof(OrderItemEntity.Id), childMetadata.KeyProperty.Name);
    }

    [Fact]
    public void ParentKeyProperty_CanBeRetrieved_ForFKPopulation()
    {
        // Arrange
        var metadata = EntityMapper.GetMetadata<OrderItemEntity>();
        var item = new OrderItemEntity { Id = 42, OrderId = 99, ProductName = "Test" };

        // Act
        var keyGetter = AccessorFactory.CreateGetter(metadata.KeyProperty);
        var parentKeyValue = keyGetter(item);

        // Assert
        Assert.NotNull(parentKeyValue);
        Assert.Equal(42, (int)parentKeyValue);
    }

    #endregion

    #region Collection Handling Tests

    [Fact]
    public void EmptyChildCollection_IsHandledCorrectly()
    {
        // Arrange
        var order = new OrderEntity
        {
            Id = 1,
            OrderNumber = "ORD-002",
            Items = new() // Empty
        };

        // Act
        var itemsGetter = AccessorFactory.CreateGetter(typeof(OrderEntity).GetProperty(nameof(OrderEntity.Items))!);
        var items = itemsGetter(order) as List<OrderItemEntity>;

        // Assert
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public void ParentWithMultipleChildren_PreservesChildCount()
    {
        // Arrange
        var order = new OrderEntity
        {
            Id = 5,
            OrderNumber = "ORD-003",
            Items = new()
            {
                new OrderItemEntity { ProductName = "Widget A" },
                new OrderItemEntity { ProductName = "Widget B" },
                new OrderItemEntity { ProductName = "Widget C" }
            }
        };

        // Act
        var itemsGetter = AccessorFactory.CreateGetter(typeof(OrderEntity).GetProperty(nameof(OrderEntity.Items))!);
        var items = itemsGetter(order) as List<OrderItemEntity>;

        // Assert
        Assert.NotNull(items);
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public void MultipleParents_WithDifferentChildCounts()
    {
        // Arrange: Create orders with varying child counts
        var orders = new List<OrderEntity>
        {
            new OrderEntity
            {
                Id = 1,
                OrderNumber = "ORD-A",
                Items = new() { new OrderItemEntity { ProductName = "Item 1" } }
            },
            new OrderEntity
            {
                Id = 2,
                OrderNumber = "ORD-B",
                Items = new()
                {
                    new OrderItemEntity { ProductName = "Item 2" },
                    new OrderItemEntity { ProductName = "Item 3" },
                    new OrderItemEntity { ProductName = "Item 4" }
                }
            },
            new OrderEntity
            {
                Id = 3,
                OrderNumber = "ORD-C",
                Items = new() // Empty
            }
        };

        // Act
        var itemCounts = orders.Select(o => o.Items.Count).ToList();

        // Assert
        Assert.Equal(new[] { 1, 3, 0 }, itemCounts);
    }

    #endregion

    #region FK Population Workflow Tests

    [Fact]
    public void FK_PopulationWorkflow_BasicFlow()
    {
        // This test simulates the FK population mechanics that GraphInsertOrchestrator will use:
        // 1. Get parent key value
        // 2. Iterate children
        // 3. Set FK property on each child
        
        // Arrange: parent and children setup
        var parentId = 10;
        var children = new List<OrderItemEntity>
        {
            new OrderItemEntity { ProductName = "Item 1" },
            new OrderItemEntity { ProductName = "Item 2" }
        };

        // Act: Simulate FK population using AccessorFactory
        var fkProperty = typeof(OrderItemEntity).GetProperty(nameof(OrderItemEntity.OrderId))!;
        var fkSetter = AccessorFactory.CreateSetter(fkProperty);

        foreach (var child in children)
        {
            fkSetter(child, parentId);
        }

        // Assert
        Assert.All(children, child => Assert.Equal(parentId, child.OrderId));
    }

    [Fact]
    public void FK_PopulationWorkflow_CompleteOrchestration()
    {
        // Complete workflow: extract parent from order, set FK on all items
        
        // Arrange
        var order = new OrderEntity
        {
            Id = 10,
            OrderNumber = "ORD-004",
            Items = new()
            {
                new OrderItemEntity { ProductName = "Item 1" },
                new OrderItemEntity { ProductName = "Item 2" }
            }
        };

        // Act: Extract parent ID and set on all children
        var parentMetadata = EntityMapper.GetMetadata<OrderEntity>();
        var parentKeyGetter = AccessorFactory.CreateGetter(parentMetadata.KeyProperty);
        var parentKeyValue = parentKeyGetter(order);

        var fkProperty = typeof(OrderItemEntity).GetProperty(nameof(OrderItemEntity.OrderId))!;
        var fkSetter = AccessorFactory.CreateSetter(fkProperty);

        foreach (var item in order.Items)
        {
            fkSetter(item, parentKeyValue);
        }

        // Assert
        Assert.All(order.Items, item => Assert.Equal(10, item.OrderId));
    }

    #endregion

    #region Debugging Tests

    [Fact]
    public void DebugEntityMapper_OrderEntity_RelationshipDetection()
    {
        // This test traces through EntityMapper.BuildMetadata() step by step
        // to identify where relationship detection might be failing
        
        var entityType = typeof(OrderEntity);

        // Step 1: Check that all properties are visible
        var allProps = entityType.GetProperties();
        Assert.NotEmpty(allProps);

        // Step 2: Check that Items property exists and has [HasMany]
        var itemsProp = allProps.FirstOrDefault(p => p.Name == nameof(OrderEntity.Items));
        Assert.NotNull(itemsProp);

        var hasMany = itemsProp.GetCustomAttribute<HasManyAttribute>();
        Assert.NotNull(hasMany);
        Assert.Equal(nameof(OrderItemEntity.OrderId), hasMany.ForeignKey);

        // Step 3: Check that we can extract the collection element type
        var propType = itemsProp.PropertyType;
        Assert.NotNull(propType);

        if (propType.IsGenericType)
        {
            var childType = propType.GetGenericArguments()[0];
            Assert.Equal(typeof(OrderItemEntity), childType);
        }
        else
        {
            Assert.Fail("Items property is not generic type");
        }

        // Step 4: Call EntityMapper and check if it detects relationships
        var metadata = EntityMapper.GetMetadata<OrderEntity>();
        Assert.NotNull(metadata);

        // Step 5: Check relationships dictionary
        // NOTE: This assertion may fail if EntityMapper.BuildMetadata() is not populating relationships
        // The infrastructure (attribute, reflection, type extraction) is all verified above
        if (metadata.Relationships.Count == 0)
        {
            // EntityMapper.BuildMetadata didn't find any relationships
            // This is being investigated - could be an issue with:
            // 1. The loop not iterating over properties with [HasMany]
            // 2. Dictionary not being populated
            // 3. Caching or compilation issue
        }
        Assert.NotEmpty(metadata.Relationships);
    }

    #endregion
}
