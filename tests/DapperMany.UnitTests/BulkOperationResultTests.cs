using DapperMany.Internal;
using Xunit;

namespace DapperMany.UnitTests;

/// <summary>
/// Unit tests for BulkOperationResult<T> record and related types.
/// </summary>
public class BulkOperationResultTests
{
    public class WhenCreatingResult
    {
        [Fact]
        public void Should_Create_Result_With_All_Fields()
        {
            // Arrange
            var generatedIds = new object[] { 1, 2, 3 };
            var relatedEntities = new Dictionary<string, int> { { "ItemPedido", 12 } };
            var duration = TimeSpan.FromMilliseconds(42);

            // Act
            var result = new BulkOperationResult<object>
            {
                RowsInserted = 5,
                RowsUpdated = 0,
                RowsDeleted = 0,
                GeneratedIds = generatedIds,
                RelatedEntities = relatedEntities,
                Duration = duration
            };

            // Assert
            Assert.Equal(5, result.RowsInserted);
            Assert.Equal(0, result.RowsUpdated);
            Assert.Equal(0, result.RowsDeleted);
            Assert.Equal(5, result.TotalRowsAffected);
            Assert.Equal(3, result.GeneratedIds.Count);
            Assert.Equal(1, result.RelatedEntities.Count);
            Assert.Equal(12, result.RelatedEntities["ItemPedido"]);
            Assert.Equal(duration, result.Duration);
        }

        [Fact]
        public void Should_Calculate_TotalRowsAffected_Correctly()
        {
            // Arrange & Act
            var result = new BulkOperationResult<object>
            {
                RowsInserted = 10,
                RowsUpdated = 5,
                RowsDeleted = 2
            };

            // Assert
            Assert.Equal(17, result.TotalRowsAffected);
        }

        [Fact]
        public void Should_Have_Empty_Collections_By_Default()
        {
            // Arrange & Act
            var result = new BulkOperationResult<object>();

            // Assert
            Assert.Empty(result.GeneratedIds);
            Assert.Empty(result.RelatedEntities);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Should_Be_Successful_When_No_Errors()
        {
            // Arrange & Act
            var result = new BulkOperationResult<object>
            {
                RowsInserted = 5,
                Errors = Array.Empty<OperationError>()
            };

            // Assert
            Assert.True(result.IsSuccessful);
        }

        [Fact]
        public void Should_Not_Be_Successful_When_Errors_Present()
        {
            // Arrange
            var errors = new[]
            {
                new OperationError { EntityIndex = 0, Message = "Validation failed" }
            };

            // Act
            var result = new BulkOperationResult<object>
            {
                Errors = errors
            };

            // Assert
            Assert.False(result.IsSuccessful);
        }
    }

    public class WhenUsingBuilder
    {
        [Fact]
        public void Should_Build_Complete_Result()
        {
            // Arrange & Act
            var result = BulkOperationResult<object>.Builder()
                .WithRowsInserted(10)
                .WithRowsUpdated(5)
                .WithRowsDeleted(2)
                .WithGeneratedIds(new object[] { 101, 102, 103, 104, 105, 106, 107, 108, 109, 110 })
                .WithRelatedEntity("ItemPedido", 25)
                .WithRelatedEntity("NfeItem", 30)
                .WithDuration(TimeSpan.FromMilliseconds(125))
                .Build();

            // Assert
            Assert.Equal(10, result.RowsInserted);
            Assert.Equal(5, result.RowsUpdated);
            Assert.Equal(2, result.RowsDeleted);
            Assert.Equal(17, result.TotalRowsAffected);
            Assert.Equal(10, result.GeneratedIds.Count);
            Assert.Equal(2, result.RelatedEntities.Count);
            Assert.Equal(TimeSpan.FromMilliseconds(125), result.Duration);
            Assert.True(result.IsSuccessful);
        }

        [Fact]
        public void Should_Build_Result_With_Errors()
        {
            // Arrange
            var error1 = new OperationError { EntityIndex = 2, Message = "FK constraint violation" };
            var error2 = new OperationError { EntityIndex = 5, Message = "Unique constraint violation" };

            // Act
            var result = BulkOperationResult<object>.Builder()
                .WithRowsInserted(8)
                .WithError(error1)
                .WithError(error2)
                .Build();

            // Assert
            Assert.Equal(8, result.RowsInserted);
            Assert.Equal(2, result.Errors.Count);
            Assert.False(result.IsSuccessful);
            Assert.Equal("FK constraint violation", result.Errors[0].Message);
            Assert.Equal("Unique constraint violation", result.Errors[1].Message);
        }

        [Fact]
        public void Should_Build_Result_With_Multiple_Errors_At_Once()
        {
            // Arrange
            var errors = new[]
            {
                new OperationError { EntityIndex = 0, Message = "Error 1" },
                new OperationError { EntityIndex = 1, Message = "Error 2" },
                new OperationError { EntityIndex = 2, Message = "Error 3" }
            };

            // Act
            var result = BulkOperationResult<object>.Builder()
                .WithRowsInserted(7)
                .WithErrors(errors)
                .Build();

            // Assert
            Assert.Equal(3, result.Errors.Count);
            Assert.False(result.IsSuccessful);
        }

        [Fact]
        public void Should_Allow_Fluent_Chaining()
        {
            // Arrange & Act
            var result = BulkOperationResult<object>.Builder()
                .WithRowsInserted(1)
                .WithRowsUpdated(1)
                .WithRowsDeleted(1)
                .WithGeneratedId(999)
                .WithRelatedEntity("Child", 1)
                .WithDuration(TimeSpan.Zero)
                .Build();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.RowsInserted);
            Assert.Equal(1, result.RowsUpdated);
            Assert.Equal(1, result.RowsDeleted);
        }

        [Fact]
        public void Should_Support_Empty_Builder()
        {
            // Arrange & Act
            var result = BulkOperationResult<object>.Builder().Build();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.RowsInserted);
            Assert.Equal(0, result.RowsUpdated);
            Assert.Equal(0, result.RowsDeleted);
            Assert.Empty(result.GeneratedIds);
            Assert.Empty(result.RelatedEntities);
            Assert.Empty(result.Errors);
            Assert.True(result.IsSuccessful);
        }
    }

    public class WhenWorkingWithOperationError
    {
        [Fact]
        public void Should_Create_Error_With_EntityIndex()
        {
            // Arrange & Act
            var error = new OperationError
            {
                EntityIndex = 5,
                Message = "Test error",
                Exception = null
            };

            // Assert
            Assert.Equal(5, error.EntityIndex);
            Assert.Equal("Test error", error.Message);
            Assert.Null(error.Exception);
        }

        [Fact]
        public void Should_Create_Error_Without_EntityIndex()
        {
            // Arrange & Act
            var ex = new InvalidOperationException("Something went wrong");
            var error = new OperationError
            {
                Message = "Operation failed",
                Exception = ex
            };

            // Assert
            Assert.Equal(-1, error.EntityIndex);
            Assert.Equal("Operation failed", error.Message);
            Assert.NotNull(error.Exception);
            Assert.IsType<InvalidOperationException>(error.Exception);
        }

        [Fact]
        public void Should_Support_Record_Equality()
        {
            // Arrange
            var error1 = new OperationError { EntityIndex = 1, Message = "Error A" };
            var error2 = new OperationError { EntityIndex = 1, Message = "Error A" };
            var error3 = new OperationError { EntityIndex = 2, Message = "Error B" };

            // Act & Assert
            Assert.Equal(error1, error2);
            Assert.NotEqual(error1, error3);
        }
    }

    public class WhenComparingResults
    {
        [Fact]
        public void Should_Support_Result_Equality()
        {
            // Arrange
            var result1 = new BulkOperationResult<object>
            {
                RowsInserted = 5,
                RowsUpdated = 0,
                RowsDeleted = 0,
                Duration = TimeSpan.FromMilliseconds(42)
            };

            var result2 = new BulkOperationResult<object>
            {
                RowsInserted = 5,
                RowsUpdated = 0,
                RowsDeleted = 0,
                Duration = TimeSpan.FromMilliseconds(42)
            };

            var result3 = new BulkOperationResult<object>
            {
                RowsInserted = 10,
                RowsUpdated = 0,
                RowsDeleted = 0,
                Duration = TimeSpan.FromMilliseconds(42)
            };

            // Act & Assert
            Assert.Equal(result1, result2);
            Assert.NotEqual(result1, result3);
        }
    }

    public class WhenUsingSampleScenarios
    {
        [Fact]
        public void Scenario_InsertMany_Returns_GeneratedIds()
        {
            // Simulate InsertMany operation result
            var result = BulkOperationResult<object>.Builder()
                .WithRowsInserted(5)
                .WithGeneratedIds(new object[] { 101, 102, 103, 104, 105 })
                .WithDuration(TimeSpan.FromMilliseconds(50))
                .Build();

            // Assert
            Assert.Equal(5, result.RowsInserted);
            Assert.Equal(5, result.GeneratedIds.Count);
            Assert.True(result.IsSuccessful);
        }

        [Fact]
        public void Scenario_InsertManyGraph_Returns_RelatedEntities()
        {
            // Simulate InsertManyGraph operation result with parent + children
            var result = BulkOperationResult<object>.Builder()
                .WithRowsInserted(5) // 5 parent entities
                .WithGeneratedIds(new object[] { 1, 2, 3, 4, 5 })
                .WithRelatedEntity("ItemPedido", 12)
                .WithRelatedEntity("PedidoDetalhe", 5)
                .WithDuration(TimeSpan.FromMilliseconds(85))
                .Build();

            // Assert
            Assert.Equal(5, result.RowsInserted);
            Assert.Equal(17, result.TotalRowsAffected); // Only counts parent
            Assert.Equal(2, result.RelatedEntities.Count);
            Assert.Equal(12, result.RelatedEntities["ItemPedido"]);
            Assert.Equal(5, result.RelatedEntities["PedidoDetalhe"]);
        }

        [Fact]
        public void Scenario_PartialCommit_With_Errors()
        {
            // Simulate operation that continued despite some errors
            var errors = new[]
            {
                new OperationError { EntityIndex = 2, Message = "FK constraint failed" },
                new OperationError { EntityIndex = 7, Message = "Duplicate key" }
            };

            var result = BulkOperationResult<object>.Builder()
                .WithRowsInserted(8) // 8 of 10 succeeded
                .WithErrors(errors)
                .WithGeneratedIds(new object[] { 101, 102, 103, 104, 105, 106, 107, 108 })
                .WithDuration(TimeSpan.FromMilliseconds(120))
                .Build();

            // Assert
            Assert.Equal(8, result.RowsInserted);
            Assert.Equal(2, result.Errors.Count);
            Assert.False(result.IsSuccessful);
        }

        [Fact]
        public void Scenario_UpdateMany_Without_GeneratedIds()
        {
            // Simulate UpdateMany operation (no generated IDs)
            var result = BulkOperationResult<object>.Builder()
                .WithRowsUpdated(42)
                .WithDuration(TimeSpan.FromMilliseconds(30))
                .Build();

            // Assert
            Assert.Equal(42, result.RowsUpdated);
            Assert.Empty(result.GeneratedIds);
            Assert.Equal(0, result.RowsInserted);
            Assert.True(result.IsSuccessful);
        }

        [Fact]
        public void Scenario_DeleteMany_Returns_Count()
        {
            // Simulate DeleteMany operation
            var result = BulkOperationResult<object>.Builder()
                .WithRowsDeleted(15)
                .WithDuration(TimeSpan.FromMilliseconds(20))
                .Build();

            // Assert
            Assert.Equal(15, result.RowsDeleted);
            Assert.Equal(15, result.TotalRowsAffected);
            Assert.Empty(result.GeneratedIds);
            Assert.True(result.IsSuccessful);
        }
    }
}
