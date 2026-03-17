using Moq;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Extensions;

public class RepositoryExtensionsTests
{
    private sealed class TestEntity : BaseEntity
    {
    }

    [Fact]
    public async Task ListByChunkedContainsAsyncWhenEmptyKeysReturnsEmptyListAndDoesNotCallRepository()
    {
        Mock<IRepository> mockRepo = new();
        List<Guid> keys = [];

        List<TestEntity> result = await mockRepo.Object.ListByChunkedContainsAsync<TestEntity, Guid>(
            keys,
            chunk => e => chunk.Contains(e.Id));

        Assert.Empty(result);
        mockRepo.Verify(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()), Times.Never);
        mockRepo.Verify(r => r.ListAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()), Times.Never);
    }

    [Fact]
    public async Task ListByChunkedContainsAsyncWhenUnder1000KeysCallsRepositoryOnce()
    {
        Mock<IRepository> mockRepo = new();
        List<Guid> keys = Enumerable.Range(0, 500).Select(_ => Guid.NewGuid()).ToList();

        mockRepo.Setup(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync([new TestEntity { Id = Guid.NewGuid() }]);

        List<TestEntity> result = await mockRepo.Object.ListByChunkedContainsAsync<TestEntity, Guid>(
            keys,
            chunk => e => chunk.Contains(e.Id));

        Assert.Single(result);
        mockRepo.Verify(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()), Times.Once);
    }

    [Fact]
    public async Task ListByChunkedContainsAsyncWhenOver1000KeysCallsRepositoryMultipleTimes()
    {
        Mock<IRepository> mockRepo = new();
        List<Guid> keys = Enumerable.Range(0, 2500).Select(_ => Guid.NewGuid()).ToList(); // Should split into 1000, 1000, 500

        mockRepo.Setup(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync([new TestEntity { Id = Guid.NewGuid() }]);

        List<TestEntity> result = await mockRepo.Object.ListByChunkedContainsAsync<TestEntity, Guid>(
            keys,
            chunk => e => chunk.Contains(e.Id));

        Assert.Equal(3, result.Count); // 1 per chunk
        mockRepo.Verify(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ListByChunkedContainsAsyncWhenAsNoTrackingFalseCallsListAsync()
    {
        Mock<IRepository> mockRepo = new();
        List<Guid> keys = [Guid.NewGuid()];

        mockRepo.Setup(r => r.ListAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync([new TestEntity { Id = Guid.NewGuid() }]);

        List<TestEntity> result = await mockRepo.Object.ListByChunkedContainsAsync<TestEntity, Guid>(
            keys,
            chunk => e => chunk.Contains(e.Id),
            asNoTracking: false);

        Assert.Single(result);
        mockRepo.Verify(r => r.ListAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()), Times.Once);
        mockRepo.Verify(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()), Times.Never);
    }

    [Fact]
    public async Task ListProjectedByChunkedContainsAsyncWhenEmptyKeysReturnsEmptyListAndDoesNotCallRepository()
    {
        Mock<IRepository> mockRepo = new();
        List<Guid> keys = [];

        List<string> result = await mockRepo.Object.ListProjectedByChunkedContainsAsync<TestEntity, string, Guid>(
            keys,
            chunk => e => chunk.Contains(e.Id),
            e => e.Id.ToString());

        Assert.Empty(result);
        mockRepo.Verify(r => r.ListProjectedAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<Expression<Func<TestEntity, string>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListProjectedByChunkedContainsAsyncWhenUnder1000KeysCallsRepositoryOnce()
    {
        Mock<IRepository> mockRepo = new();
        List<Guid> keys = Enumerable.Range(0, 500).Select(_ => Guid.NewGuid()).ToList();

        mockRepo.Setup(r => r.ListProjectedAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<Expression<Func<TestEntity, string>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(["TestResult"]);

        List<string> result = await mockRepo.Object.ListProjectedByChunkedContainsAsync<TestEntity, string, Guid>(
            keys,
            chunk => e => chunk.Contains(e.Id),
            e => e.Id.ToString());

        Assert.Single(result);
        Assert.Equal("TestResult", result[0]);
        mockRepo.Verify(r => r.ListProjectedAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<Expression<Func<TestEntity, string>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListProjectedByChunkedContainsAsyncWhenOver1000KeysCallsRepositoryMultipleTimes()
    {
        Mock<IRepository> mockRepo = new();
        List<Guid> keys = Enumerable.Range(0, 2500).Select(_ => Guid.NewGuid()).ToList(); // Should split into 1000, 1000, 500

        mockRepo.Setup(r => r.ListProjectedAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<Expression<Func<TestEntity, string>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(["TestResult"]);

        List<string> result = await mockRepo.Object.ListProjectedByChunkedContainsAsync<TestEntity, string, Guid>(
            keys,
            chunk => e => chunk.Contains(e.Id),
            e => e.Id.ToString());

        Assert.Equal(3, result.Count);
        mockRepo.Verify(r => r.ListProjectedAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<Expression<Func<TestEntity, string>>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task IsValidContactAsyncWhenIdIsEmptyReturnsFalseAndDoesNotCallRepository()
    {
        Mock<IRepository> mockRepo = new();

        bool result = await mockRepo.Object.IsValidContactAsync(Guid.Empty);

        Assert.False(result);
        mockRepo.Verify(r => r.CountAsync(It.IsAny<Expression<Func<Rvnx.CRM.Core.Models.Contact.Contact, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IsValidContactAsyncWhenContactExistsAndIsNotPartialReturnsTrue()
    {
        Mock<IRepository> mockRepo = new();
        Guid contactId = Guid.NewGuid();

        mockRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Rvnx.CRM.Core.Models.Contact.Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        bool result = await mockRepo.Object.IsValidContactAsync(contactId);

        Assert.True(result);
        mockRepo.Verify(r => r.CountAsync(It.IsAny<Expression<Func<Rvnx.CRM.Core.Models.Contact.Contact, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsValidContactAsyncWhenContactDoesNotExistOrIsPartialReturnsFalse()
    {
        Mock<IRepository> mockRepo = new();
        Guid contactId = Guid.NewGuid();

        mockRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Rvnx.CRM.Core.Models.Contact.Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        bool result = await mockRepo.Object.IsValidContactAsync(contactId);

        Assert.False(result);
        mockRepo.Verify(r => r.CountAsync(It.IsAny<Expression<Func<Rvnx.CRM.Core.Models.Contact.Contact, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}