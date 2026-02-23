using Moq;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Extensions
{
    public class RepositoryExtensionsTests
    {
        private sealed class TestEntity : BaseEntity
        {
            public Guid GroupId { get; set; }
        }

        [Fact]
        public async Task ListByChunkedContainsAsyncWhenEmptyKeysReturnsEmptyListAndDoesNotCallRepository()
        {
            // Arrange
            var mockRepo = new Mock<IRepository>();
            var keys = new List<Guid>();

            // Act
            var result = await mockRepo.Object.ListByChunkedContainsAsync<TestEntity, Guid>(
                keys,
                chunk => e => chunk.Contains(e.GroupId));

            // Assert
            Assert.Empty(result);
            mockRepo.Verify(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()), Times.Never);
            mockRepo.Verify(r => r.ListAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task ListByChunkedContainsAsyncWhenUnder1000KeysCallsRepositoryOnce()
        {
            // Arrange
            var mockRepo = new Mock<IRepository>();
            var keys = Enumerable.Range(0, 500).Select(_ => Guid.NewGuid()).ToList();
            
            mockRepo.Setup(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync(new List<TestEntity> { new TestEntity { Id = Guid.NewGuid() } });

            // Act
            var result = await mockRepo.Object.ListByChunkedContainsAsync<TestEntity, Guid>(
                keys,
                chunk => e => chunk.Contains(e.GroupId));

            // Assert
            Assert.Single(result);
            mockRepo.Verify(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task ListByChunkedContainsAsyncWhenOver1000KeysCallsRepositoryMultipleTimes()
        {
            // Arrange
            var mockRepo = new Mock<IRepository>();
            var keys = Enumerable.Range(0, 2500).Select(_ => Guid.NewGuid()).ToList(); // Should split into 1000, 1000, 500
            
            mockRepo.Setup(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync(new List<TestEntity> { new TestEntity { Id = Guid.NewGuid() } });

            // Act
            var result = await mockRepo.Object.ListByChunkedContainsAsync<TestEntity, Guid>(
                keys,
                chunk => e => chunk.Contains(e.GroupId));

            // Assert
            Assert.Equal(3, result.Count); // 1 per chunk
            mockRepo.Verify(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()), Times.Exactly(3));
        }

        [Fact]
        public async Task ListByChunkedContainsAsyncWhenAsNoTrackingFalseCallsListAsync()
        {
            // Arrange
            var mockRepo = new Mock<IRepository>();
            var keys = new List<Guid> { Guid.NewGuid() };
            
            mockRepo.Setup(r => r.ListAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync(new List<TestEntity> { new TestEntity { Id = Guid.NewGuid() } });

            // Act
            var result = await mockRepo.Object.ListByChunkedContainsAsync<TestEntity, Guid>(
                keys,
                chunk => e => chunk.Contains(e.GroupId),
                asNoTracking: false);

            // Assert
            Assert.Single(result);
            mockRepo.Verify(r => r.ListAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()), Times.Once);
            mockRepo.Verify(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()), Times.Never);
        }
    }
}
