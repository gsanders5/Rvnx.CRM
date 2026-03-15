using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Infrastructure.Services;
using System.Security.Claims;

namespace Rvnx.CRM.Tests.Services;

public class UserSynchronizationTests
{
    private static CRMDbContext GetInMemoryDbContext()
    {
        DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Mock<ICurrentUserService> mockUserService = new();
        mockUserService.Setup(u => u.UserId).Returns((Guid?)null); // Setup as system for syncing
        mockUserService.Setup(u => u.UserName).Returns("System");

        CRMDbContext context = new(options, mockUserService.Object);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task SyncUserAsyncNewUserShouldCreateUser()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repository = new(context);
        UserSynchronizationService service = new(context, repository);

        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, "sub123"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "Test User")
        ];
        ClaimsIdentity identity = new(claims, "TestAuth");
        ClaimsPrincipal principal = new(identity);

        await service.SyncUserAsync(principal);

        Core.Models.User? user = await repository.QueryUnfiltered<Core.Models.User>().FirstOrDefaultAsync(u => u.SubjectId == "sub123");
        Assert.NotNull(user);
        Assert.Equal("test@example.com", user.Email);
        Assert.Equal("Test User", user.DisplayName);

        Assert.Contains(principal.Claims, c =>
            c.Type == ClaimConstants.InternalUserIdClaimType &&
            c.Value == user.Id.ToString());
    }

    [Fact]
    public async Task SyncUserAsyncNewUserShouldPreserveOriginalNameIdentifier()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repository = new(context);
        UserSynchronizationService service = new(context, repository);

        const string originalSubject = "external-idp-sub-123";
        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, originalSubject),
            new Claim(ClaimTypes.Email, "test@example.com")
        ];
        ClaimsIdentity identity = new(claims, "TestAuth");
        ClaimsPrincipal principal = new(identity);

        await service.SyncUserAsync(principal);

        Assert.Contains(principal.Claims, c =>
            c.Type == ClaimTypes.NameIdentifier &&
            c.Value == originalSubject);

        // Internal ID should be in separate claim
        Core.Models.User? user = await repository.QueryUnfiltered<Core.Models.User>().FirstOrDefaultAsync(u => u.SubjectId == originalSubject);
        Assert.NotNull(user);
        Assert.Contains(principal.Claims, c =>
            c.Type == ClaimConstants.InternalUserIdClaimType &&
            c.Value == user.Id.ToString());
    }

    [Fact]
    public async Task SyncUserAsyncExistingUserShouldUpdateDetailsAndAddInternalIdClaim()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repository = new(context);
        UserSynchronizationService service = new(context, repository);

        Core.Models.User existingUser = new()
        {
            SubjectId = "sub456",
            Email = "old@example.com",
            DisplayName = "Old Name",
            Group = new Core.Models.UserGroup { Name = "Old Group" }
        };
        context.Users!.Add(existingUser);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear(); // Clear tracker to simulate fresh request

        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, "sub456"),
            new Claim(ClaimTypes.Email, "new@example.com"),
            new Claim(ClaimTypes.Name, "New Name")
        ];
        ClaimsIdentity identity = new(claims, "TestAuth");
        ClaimsPrincipal principal = new(identity);

        await service.SyncUserAsync(principal);

        Core.Models.User? user = await repository.QueryUnfiltered<Core.Models.User>().FirstOrDefaultAsync(u => u.SubjectId == "sub456");
        Assert.NotNull(user);
        Assert.Equal("new@example.com", user.Email); // Should update
        Assert.Equal("New Name", user.DisplayName); // Should update

        // Original NameIdentifier should be preserved
        Assert.Contains(principal.Claims, c =>
            c.Type == ClaimTypes.NameIdentifier &&
            c.Value == "sub456");

        // Internal ID should be in separate claim
        Assert.Contains(principal.Claims, c =>
            c.Type == ClaimConstants.InternalUserIdClaimType &&
            c.Value == existingUser.Id.ToString());
    }

    [Fact]
    public async Task SyncUserAsyncShouldAddNameClaimWhenNotPresent()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repository = new(context);
        UserSynchronizationService service = new(context, repository);

        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, "sub789"),
            new Claim(ClaimTypes.Email, "test@example.com")
        ];
        ClaimsIdentity identity = new(claims, "TestAuth");
        ClaimsPrincipal principal = new(identity);

        await service.SyncUserAsync(principal);

        Core.Models.User? user = await repository.QueryUnfiltered<Core.Models.User>().FirstOrDefaultAsync(u => u.SubjectId == "sub789");
        Assert.NotNull(user);

        // Name claim should be added from DisplayName
        Assert.Contains(principal.Claims, c =>
            c.Type == ClaimTypes.Name &&
            c.Value == user.DisplayName);
    }

    [Fact]
    public async Task SyncUserAsyncShouldNotAddNameClaimWhenAlreadyPresent()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repository = new(context);
        UserSynchronizationService service = new(context, repository);

        const string existingName = "Existing Name";
        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, "sub999"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, existingName)
        ];
        ClaimsIdentity identity = new(claims, "TestAuth");
        ClaimsPrincipal principal = new(identity);

        await service.SyncUserAsync(principal);

        List<Claim> nameClaims = principal.Claims.Where(c => c.Type == ClaimTypes.Name).ToList();
        Assert.Single(nameClaims);
        Assert.Equal(existingName, nameClaims[0].Value);
    }

    [Fact]
    public async Task SyncUserAsyncShouldDoNothingWhenNoSubject()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repository = new(context);
        UserSynchronizationService service = new(context, repository);

        // No NameIdentifier claim
        List<Claim> claims =
        [
            new Claim(ClaimTypes.Email, "test@example.com")
        ];
        ClaimsIdentity identity = new(claims, "TestAuth");
        ClaimsPrincipal principal = new(identity);

        await service.SyncUserAsync(principal);

        int userCount = await repository.QueryUnfiltered<Core.Models.User>().CountAsync();
        Assert.Equal(0, userCount);

        Assert.DoesNotContain(principal.Claims, c =>
            c.Type == ClaimConstants.InternalUserIdClaimType);
    }

    [Fact]
    public async Task SyncUserAsyncShouldReplaceInternalIdClaimWhenCalledMultipleTimes()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repository = new(context);
        UserSynchronizationService service = new(context, repository);

        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, "sub-multiple"),
            new Claim(ClaimTypes.Email, "test@example.com")
        ];
        ClaimsIdentity identity = new(claims, "TestAuth");
        ClaimsPrincipal principal = new(identity);

        await service.SyncUserAsync(principal);
        await service.SyncUserAsync(principal);
        await service.SyncUserAsync(principal);

        List<Claim> internalIdClaims = principal.Claims
            .Where(c => c.Type == ClaimConstants.InternalUserIdClaimType)
            .ToList();
        Assert.Single(internalIdClaims);
    }

    [Fact]
    public async Task SyncUserAsyncShouldUseSubClaimWhenNameIdentifierNotPresent()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repository = new(context);
        UserSynchronizationService service = new(context, repository);

        // Use "sub" claim instead of NameIdentifier (common in OAuth2/OIDC)
        List<Claim> claims =
        [
            new Claim("sub", "oauth-subject-id"),
            new Claim("email", "oauth@example.com")
        ];
        ClaimsIdentity identity = new(claims, "TestAuth");
        ClaimsPrincipal principal = new(identity);

        await service.SyncUserAsync(principal);

        Core.Models.User? user = await repository.QueryUnfiltered<Core.Models.User>().FirstOrDefaultAsync(u => u.SubjectId == "oauth-subject-id");
        Assert.NotNull(user);
        Assert.Equal("oauth@example.com", user.Email);
    }
}