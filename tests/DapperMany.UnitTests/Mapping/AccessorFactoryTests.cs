using DapperMany.Internal.Mapping;
using Xunit;

namespace DapperMany.UnitTests.Mapping;

public class AccessorFactoryTests
{
    private class TestEntity
    {
        public int IntProperty { get; set; }
        public string? StringProperty { get; set; }
        public DateTime DateProperty { get; set; }
    }

    [Fact]
    public void CreateGetter_ReturnsValueCorrectly()
    {
        var prop = typeof(TestEntity).GetProperty(nameof(TestEntity.IntProperty))!;
        var getter = AccessorFactory.CreateGetter(prop);

        var entity = new TestEntity { IntProperty = 42 };
        var value = getter(entity);

        Assert.Equal(42, value);
    }

    [Fact]
    public void CreateGetter_WithNullValue_ReturnsNull()
    {
        var prop = typeof(TestEntity).GetProperty(nameof(TestEntity.StringProperty))!;
        var getter = AccessorFactory.CreateGetter(prop);

        var entity = new TestEntity { StringProperty = null };
        var value = getter(entity);

        Assert.Null(value);
    }

    [Fact]
    public void CreateGetter_CachesCompiled()
    {
        var prop = typeof(TestEntity).GetProperty(nameof(TestEntity.IntProperty))!;
        var getter1 = AccessorFactory.CreateGetter(prop);
        var getter2 = AccessorFactory.CreateGetter(prop);

        Assert.Same(getter1, getter2); // Same compiled delegate instance
    }

    [Fact]
    public void CreateSetter_SetsValueCorrectly()
    {
        var prop = typeof(TestEntity).GetProperty(nameof(TestEntity.IntProperty))!;
        var setter = AccessorFactory.CreateSetter(prop);

        var entity = new TestEntity();
        setter(entity, 99);

        Assert.Equal(99, entity.IntProperty);
    }

    [Fact]
    public void CreateSetter_WithNullValue_SetsNull()
    {
        var prop = typeof(TestEntity).GetProperty(nameof(TestEntity.StringProperty))!;
        var setter = AccessorFactory.CreateSetter(prop);

        var entity = new TestEntity { StringProperty = "initial" };
        setter(entity, null);

        Assert.Null(entity.StringProperty);
    }

    [Fact]
    public void CreateSetter_CachesCompiled()
    {
        var prop = typeof(TestEntity).GetProperty(nameof(TestEntity.IntProperty))!;
        var setter1 = AccessorFactory.CreateSetter(prop);
        var setter2 = AccessorFactory.CreateSetter(prop);

        Assert.Same(setter1, setter2); // Same compiled delegate instance
    }

    [Fact]
    public void CompiledGetter_WithDifferentDataTypes()
    {
        // Test compiled getters work with various data types
        var intProp = typeof(TestEntity).GetProperty(nameof(TestEntity.IntProperty))!;
        var stringProp = typeof(TestEntity).GetProperty(nameof(TestEntity.StringProperty))!;
        var dateProp = typeof(TestEntity).GetProperty(nameof(TestEntity.DateProperty))!;

        var intGetter = AccessorFactory.CreateGetter(intProp);
        var stringGetter = AccessorFactory.CreateGetter(stringProp);
        var dateGetter = AccessorFactory.CreateGetter(dateProp);

        var entity = new TestEntity
        {
            IntProperty = 99,
            StringProperty = "Test",
            DateProperty = new DateTime(2024, 1, 1)
        };

        Assert.Equal(99, intGetter(entity));
        Assert.Equal("Test", stringGetter(entity));
        Assert.Equal(new DateTime(2024, 1, 1), dateGetter(entity));
    }

    [Fact]
    public void Concurrent_GettersAndSetters_ThreadSafe()
    {
        var prop = typeof(TestEntity).GetProperty(nameof(TestEntity.IntProperty))!;
        var getters = new System.Collections.Concurrent.ConcurrentBag<Delegate>();

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() =>
            {
                var getter = AccessorFactory.CreateGetter(prop);
                getters.Add(getter);
            }))
            .ToArray();

        Task.WaitAll(tasks);

        // All should be the same compiled instance
        var uniqueInstances = getters.Distinct().Count();
        Assert.Equal(1, uniqueInstances);
    }
}
