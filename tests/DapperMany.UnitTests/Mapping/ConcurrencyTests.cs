using DapperMany.Internal.Mapping;
using Xunit;

namespace DapperMany.UnitTests.Mapping;

public class ConcurrencyTests
{
    [System.ComponentModel.DataAnnotations.Schema.Table("TestTable")]
    private class TestEntity
    {
        [System.ComponentModel.DataAnnotations.Key]
        [System.ComponentModel.DataAnnotations.Schema.DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string? Name { get; set; }
    }

    [Fact]
    public void EntityMapper_ConcurrentReads_NoRaceConditions()
    {
        const int threadCount = 50;
        const int callsPerThread = 100;

        var results = new System.Collections.Concurrent.ConcurrentBag<EntityMetadata>();
        var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, threadCount)
            .Select(_ => Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < callsPerThread; i++)
                    {
                        var metadata = EntityMapper.GetMetadata<TestEntity>();
                        results.Add(metadata);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }))
            .ToArray();

        Task.WaitAll(tasks);

        // No exceptions
        Assert.Empty(errors);

        // All results should be the same instance (proves caching worked)
        var uniqueInstances = results.Distinct().Count();
        Assert.Equal(1, uniqueInstances);

        // Total calls = threadCount * callsPerThread
        Assert.Equal(threadCount * callsPerThread, results.Count);
    }

    [Fact]
    public void AccessorFactory_ConcurrentCompilation_NoDuplicateDelegates()
    {
        var prop = typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!;
        var compiledDelegates = new System.Collections.Concurrent.ConcurrentBag<object>();
        var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        const int threadCount = 50;

        var tasks = Enumerable.Range(0, threadCount)
            .Select(_ => Task.Run(() =>
            {
                try
                {
                    var getter = AccessorFactory.CreateGetter(prop);
                    compiledDelegates.Add(getter);
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }))
            .ToArray();

        Task.WaitAll(tasks);

        Assert.Empty(errors);

        // All delegates should be the exact same instance (proves caching prevented compilation duplication)
        var uniqueInstances = compiledDelegates.Distinct().Count();
        Assert.Equal(1, uniqueInstances);
    }

    [Fact]
    public void Mixed_EntityMapper_And_AccessorFactory_ConcurrentAccess()
    {
        const int threads = 20;
        var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, threads)
            .Select(i => Task.Run(() =>
            {
                try
                {
                    // Some threads read metadata
                    if (i % 2 == 0)
                    {
                        for (int j = 0; j < 50; j++)
                        {
                            var metadata = EntityMapper.GetMetadata<TestEntity>();
                            Assert.NotNull(metadata);
                        }
                    }
                    else
                    {
                        // Other threads compile accessors
                        var prop = typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!;
                        for (int j = 0; j < 50; j++)
                        {
                            var getter = AccessorFactory.CreateGetter(prop);
                            var setter = AccessorFactory.CreateSetter(prop);
                            Assert.NotNull(getter);
                            Assert.NotNull(setter);
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }))
            .ToArray();

        Task.WaitAll(tasks);

        Assert.Empty(errors);
    }
}
