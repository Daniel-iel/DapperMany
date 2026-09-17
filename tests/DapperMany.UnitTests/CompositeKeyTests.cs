namespace DapperMany.UnitTests;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DapperMany.Internal.Mapping;
using Xunit;

/// <summary>
/// Tests for composite key support in EntityMetadata and EntityMapper.
/// </summary>
public class CompositeKeyTests
{
    #region Test Entities

    [Table("SingleKeyEntity")]
    private class SingleKeyEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Name { get; set; }
    }

    [Table("CompositeKeyEntity")]
    private class CompositeKeyEntity
    {
        [Key]
        public int TenantId { get; set; }

        [Key]
        public string DocumentNumber { get; set; }

        public string Description { get; set; }
    }

    [Table("InvalidCompositeKeyWithIdentity")]
    private class InvalidCompositeKeyWithIdentity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TenantId { get; set; }

        [Key]
        public string DocumentNumber { get; set; }
    }

    [Table("NoKeyEntity")]
    private class NoKeyEntity
    {
        public string Name { get; set; }
    }

    [Table("TenantOrderEntity")]
    private class TenantOrderEntity
    {
        [Key]
        public int TenantId { get; set; }

        [Key]
        public int OrderId { get; set; }

        [Key]
        public string Region { get; set; }

        public DateTime OrderDate { get; set; }
    }

    #endregion

    [Fact]
    public void SingleKeyEntity_HasOneKeyProperty()
    {
        // Arrange & Act
        var metadata = EntityMapper.GetMetadata<SingleKeyEntity>();

        // Assert
        Assert.Single(metadata.KeyProperties);
        Assert.False(metadata.IsCompositeKey);
        Assert.Equal("Id", metadata.KeyProperty.Name);
        Assert.Equal("Id", metadata.KeyProperties[0].Name);
    }

    [Fact]
    public void CompositeKeyEntity_HasMultipleKeyProperties()
    {
        // Arrange & Act
        var metadata = EntityMapper.GetMetadata<CompositeKeyEntity>();

        // Assert
        Assert.Equal(2, metadata.KeyProperties.Count);
        Assert.True(metadata.IsCompositeKey);
        
        // Keys are ordered by name
        Assert.Equal("DocumentNumber", metadata.KeyProperties[0].Name);
        Assert.Equal("TenantId", metadata.KeyProperties[1].Name);
    }

    [Fact]
    public void CompositeKeyEntity_BackwardCompatibilityKeyProperty_ReturnsFirstKey()
    {
        // Arrange & Act
        var metadata = EntityMapper.GetMetadata<CompositeKeyEntity>();

        // Assert
        // KeyProperty should return first key for backward compatibility
        Assert.Equal("DocumentNumber", metadata.KeyProperty.Name);
    }

    [Fact]
    public void TripleCompositeKey_AllKeysCollected()
    {
        // Arrange & Act
        var metadata = EntityMapper.GetMetadata<TenantOrderEntity>();

        // Assert
        Assert.Equal(3, metadata.KeyProperties.Count);
        Assert.True(metadata.IsCompositeKey);

        // Verify keys are in alphabetical order
        var keyNames = new[] { "OrderId", "Region", "TenantId" };
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(keyNames[i], metadata.KeyProperties[i].Name);
        }
    }

    [Fact]
    public void CompositeKeyWithIdentity_ThrowsValidationException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => EntityMapper.GetMetadata<InvalidCompositeKeyWithIdentity>()
        );

        // Verify error message contains relevant information
        Assert.Contains("Composite Key", exception.Message);
        Assert.Contains("DatabaseGenerated", exception.Message);
    }

    [Fact]
    public void NoKeyEntity_ThrowsValidationException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => EntityMapper.GetMetadata<NoKeyEntity>()
        );

        // Assert
        Assert.Contains("[Key]", exception.Message);
    }

    [Fact]
    public void KeyPropertiesAreInMappedProperties()
    {
        // Arrange & Act
        var metadata = EntityMapper.GetMetadata<CompositeKeyEntity>();

        // Assert
        foreach (var keyProp in metadata.KeyProperties)
        {
            Assert.Contains(keyProp, metadata.MappedProperties);
        }
    }

    [Fact]
    public void CompositeKeyMetadata_AllPropertiesReadOnly()
    {
        // Arrange & Act
        var metadata = EntityMapper.GetMetadata<CompositeKeyEntity>();

        // Assert
        Assert.NotNull(metadata.KeyProperties);
        var list = (System.Collections.ObjectModel.ReadOnlyCollection<System.Reflection.PropertyInfo>)metadata.KeyProperties;
        Assert.IsType<System.Collections.ObjectModel.ReadOnlyCollection<System.Reflection.PropertyInfo>>(list);
    }

    [Fact]
    public void MetadataCacheWorksWithCompositeKeys()
    {
        // Arrange & Act
        var metadata1 = EntityMapper.GetMetadata<CompositeKeyEntity>();
        var metadata2 = EntityMapper.GetMetadata<CompositeKeyEntity>();

        // Assert
        // Should return same instance from cache
        Assert.Same(metadata1, metadata2);
        Assert.Equal(2, metadata1.KeyProperties.Count);
    }
}
