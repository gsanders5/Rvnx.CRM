using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;

namespace Rvnx.CRM.Tests.Repositories
{
    public class RepositoryDeleteAsyncTests
    {
        // BadEntity has no parameterless constructor
        public class BadEntity : BaseEntity
        {
            public BadEntity(string someParam)
            {
                SomeParam = someParam;
            }

            public string SomeParam { get; set; }
        }

        public class TestDbContext : CRMDbContext
        {
            public TestDbContext(DbContextOptions<CRMDbContext> options, ICurrentUserService currentUserService)
                : base(options, currentUserService)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                modelBuilder.Entity<BadEntity>();
            }
        }

        private static TestDbContext GetInMemoryDbContext()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Mock<ICurrentUserService> mockUserService = new();
            mockUserService.Setup(u => u.UserId).Returns((Guid?)null);
            mockUserService.Setup(u => u.UserName).Returns("TestUser");

            TestDbContext context = new(options, mockUserService.Object);
            return context;
        }

        [Fact]
        public async Task DeleteAsyncShouldSucceedWhenEntityDoesNotExist()
        {
            using TestDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);
            Guid id = Guid.NewGuid();

            await repo.DeleteAsync<BadEntity>(id);
        }

        [Fact]
        public async Task DeleteAsyncShouldDeleteWhenEntityExists()
        {
            using TestDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);

            BadEntity entity = new("test");
            await repo.AddAsync(entity);
            await repo.SaveChangesAsync();

            // Detach to ensure DeleteAsync fetches it or handles it correctly
            context.Entry(entity).State = EntityState.Detached;

            await repo.DeleteAsync<BadEntity>(entity.Id);
            await repo.SaveChangesAsync();

            BadEntity? deleted = await context.Set<BadEntity>().FindAsync(entity.Id);
            Assert.Null(deleted);
        }
    }
}