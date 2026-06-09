using Microsoft.Extensions.Caching.Memory;
using Moq;
using Rvnx.CRM.Core.DTOs.Immich;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Infrastructure.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services;

public class ImmichSettingsServiceTests
{
    private readonly Mock<IRepository> _mockRepo = new();
    private readonly ImmichSettingsService _service;

    public ImmichSettingsServiceTests()
    {
        _service = new ImmichSettingsService(_mockRepo.Object, new MemoryCache(new MemoryCacheOptions()));
    }

    private void SetupStored(params GroupImmichSettings[] rows)
    {
        _mockRepo.Setup(r => r.ListAsync(It.IsAny<Expression<Func<GroupImmichSettings, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows.ToList());
        _mockRepo.Setup(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<GroupImmichSettings, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync(rows.ToList());
    }

    [Fact]
    public async Task GetSettingsAsyncReturnsNullWhenNotConfigured()
    {
        SetupStored();

        Assert.Null(await _service.GetSettingsAsync());
    }

    [Fact]
    public async Task GetSettingsAsyncMasksApiKey()
    {
        SetupStored(new GroupImmichSettings { Enabled = true, BaseUrl = "https://immich.example.com/api", ApiKey = "secret-key-3kfa" });

        ImmichSettingsDto? dto = await _service.GetSettingsAsync();

        Assert.NotNull(dto);
        Assert.True(dto!.Enabled);
        Assert.Equal("https://immich.example.com/api", dto.BaseUrl);
        Assert.Equal("••••3kfa", dto.ApiKeyHint);
        Assert.DoesNotContain("secret", dto.ApiKeyHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetConnectionAsyncReturnsRawKeyAndGroupId()
    {
        Guid groupId = Guid.NewGuid();
        SetupStored(new GroupImmichSettings { GroupId = groupId, Enabled = true, BaseUrl = "https://x/api", ApiKey = "raw-key" });

        ImmichConnectionDto? conn = await _service.GetConnectionAsync();

        Assert.NotNull(conn);
        Assert.Equal(groupId, conn!.GroupId);
        Assert.Equal("raw-key", conn.ApiKey);
    }

    [Fact]
    public async Task SaveAsyncFailsWhenBaseUrlMissing()
    {
        ImmichSettingsOperationResult result = await _service.SaveAsync(true, "  ", "key");

        Assert.False(result.Success);
        Assert.Contains("Server URL is required.", result.Errors);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://immich.example.com/api")]
    [InlineData("/relative/path")]
    public async Task SaveAsyncFailsWhenBaseUrlInvalid(string baseUrl)
    {
        ImmichSettingsOperationResult result = await _service.SaveAsync(true, baseUrl, "key");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("absolute http(s) URL", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SaveAsyncFailsOnCreateWithoutApiKey()
    {
        SetupStored();

        ImmichSettingsOperationResult result = await _service.SaveAsync(true, "https://immich.example.com/api", "");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("API key is required", StringComparison.Ordinal));
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<GroupImmichSettings>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsyncCreatesNewRowWhenNoneStored()
    {
        SetupStored();
        GroupImmichSettings? added = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<GroupImmichSettings>(), It.IsAny<CancellationToken>()))
            .Callback<GroupImmichSettings, CancellationToken>((e, _) => added = e)
            .ReturnsAsync((GroupImmichSettings e, CancellationToken _) => e);

        ImmichSettingsOperationResult result = await _service.SaveAsync(true, "https://immich.example.com/api ", " new-key ");

        Assert.True(result.Success);
        Assert.NotNull(added);
        Assert.True(added!.Enabled);
        Assert.Equal("https://immich.example.com/api", added.BaseUrl);
        Assert.Equal("new-key", added.ApiKey);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsyncUpdatesExistingAndKeepsKeyWhenBlank()
    {
        GroupImmichSettings existing = new() { Id = Guid.NewGuid(), Enabled = false, BaseUrl = "https://old/api", ApiKey = "old-key" };
        SetupStored(existing);
        _mockRepo.Setup(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        ImmichSettingsOperationResult result = await _service.SaveAsync(true, "https://new.example.com/api", null);

        Assert.True(result.Success);
        Assert.Equal(existing.Id, result.SettingsId);
        Assert.True(existing.Enabled);
        Assert.Equal("https://new.example.com/api", existing.BaseUrl);
        Assert.Equal("old-key", existing.ApiKey);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<GroupImmichSettings>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsyncReplacesKeyWhenProvided()
    {
        GroupImmichSettings existing = new() { Id = Guid.NewGuid(), Enabled = true, BaseUrl = "https://old/api", ApiKey = "old-key" };
        SetupStored(existing);
        _mockRepo.Setup(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        ImmichSettingsOperationResult result = await _service.SaveAsync(true, "https://old/api", "rotated-key");

        Assert.True(result.Success);
        Assert.Equal("rotated-key", existing.ApiKey);
    }

    [Fact]
    public async Task DeleteAsyncReturnsNotFoundWhenNoneStored()
    {
        SetupStored();

        ImmichSettingsOperationResult result = await _service.DeleteAsync();

        Assert.False(result.Success);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task DeleteAsyncRemovesStoredRow()
    {
        GroupImmichSettings existing = new() { Id = Guid.NewGuid(), BaseUrl = "https://old/api", ApiKey = "k" };
        SetupStored(existing);

        ImmichSettingsOperationResult result = await _service.DeleteAsync();

        Assert.True(result.Success);
        _mockRepo.Verify(r => r.DeleteAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
