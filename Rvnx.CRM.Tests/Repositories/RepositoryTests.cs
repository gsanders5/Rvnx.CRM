using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;

namespace Rvnx.CRM.Tests.Repositories;

public class RepositoryTests
{
    public class General
    {
        private static CRMDbContext GetInMemoryDbContext()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Mock<ICurrentUserService> mockUserService = new();
            mockUserService.Setup(u => u.UserId).Returns((Guid?)null);
            mockUserService.Setup(u => u.UserName).Returns("TestUser");

            CRMDbContext context = new(options, mockUserService.Object);
            return context;
        }

        [Fact]
        public async Task ListAsyncPredicateShouldFilterCorrectly()
        {
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);

            await repo.AddAsync(new Contact { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" });
            await repo.AddAsync(new Contact { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe" });
            await repo.AddAsync(new Contact { Id = Guid.NewGuid(), FirstName = "Bob", LastName = "Smith" });
            await repo.SaveChangesAsync();

            List<Contact> does = await repo.ListAsync<Contact>(c => c.LastName == "Doe");

            Assert.Equal(2, does.Count);
            Assert.Contains(does, c => c.FirstName == "John");
            Assert.Contains(does, c => c.FirstName == "Jane");
        }

        [Fact]
        public async Task ListAsyncPredicateAndIncludesShouldIncludeRelatedEntities()
        {
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);

            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "John", LastName = "Doe" };
            await repo.AddAsync(contact);
            await repo.SaveChangesAsync();

            Employer employer = new()
            {
                Id = Guid.NewGuid(),
                CompanyName = "Acme Corp",
                EmployeeId = contactId
            };
            await repo.AddAsync(employer);
            await repo.SaveChangesAsync();

            List<Contact> result = await repo.ListAsNoTrackingAsync<Contact>(c => c.LastName == "Doe", default, "Employers");

            Contact fetchedContact = result.Single();
            Assert.NotNull(fetchedContact.Employers);
            Assert.NotEmpty(fetchedContact.Employers);
            Assert.Equal("Acme Corp", fetchedContact.Employers.First().CompanyName);
        }

        [Fact]
        public async Task CountAsyncPredicateShouldCountCorrectly()
        {
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);

            await repo.AddAsync(new Contact { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" });
            await repo.AddAsync(new Contact { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe" });
            await repo.AddAsync(new Contact { Id = Guid.NewGuid(), FirstName = "Bob", LastName = "Smith" });
            await repo.SaveChangesAsync();

            int count = await repo.CountAsync<Contact>(c => c.LastName == "Doe");

            Assert.Equal(2, count);
        }

        [Fact]
        public async Task ListProjectedAsyncReturnsProjectedData()
        {
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);

            await repo.AddAsync(new Contact { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" });
            await repo.AddAsync(new Contact { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe" });
            await repo.AddAsync(new Contact { Id = Guid.NewGuid(), FirstName = "Bob", LastName = "Smith" });
            await repo.SaveChangesAsync();

            List<string> result = await repo.ListProjectedAsync<Contact, string>(
                c => c.LastName == "Doe",
                c => c.FirstName + " " + c.LastName);

            Assert.Equal(2, result.Count);
            Assert.Contains("John Doe", result);
            Assert.Contains("Jane Doe", result);
        }

        [Fact]
        public async Task ListProjectedAsyncWithOrderByOrdersDataCorrectly()
        {
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);

            await repo.AddAsync(new Contact { Id = Guid.NewGuid(), FirstName = "Charlie", LastName = "Doe" });
            await repo.AddAsync(new Contact { Id = Guid.NewGuid(), FirstName = "Alice", LastName = "Doe" });
            await repo.AddAsync(new Contact { Id = Guid.NewGuid(), FirstName = "Bob", LastName = "Doe" });
            await repo.SaveChangesAsync();

            List<string> resultAsc = await repo.ListProjectedAsync<Contact, string, string>(
                c => c.LastName == "Doe",
                c => c.FirstName,
                c => c.FirstName,
                descending: false);

            List<string> resultDesc = await repo.ListProjectedAsync<Contact, string, string>(
                c => c.LastName == "Doe",
                c => c.FirstName,
                c => c.FirstName,
                descending: true);

            Assert.Equal(3, resultAsc.Count);
            Assert.Equal("Alice", resultAsc[0]);
            Assert.Equal("Bob", resultAsc[1]);
            Assert.Equal("Charlie", resultAsc[2]);

            Assert.Equal(3, resultDesc.Count);
            Assert.Equal("Charlie", resultDesc[0]);
            Assert.Equal("Bob", resultDesc[1]);
            Assert.Equal("Alice", resultDesc[2]);
        }
    }

    public class DeleteAsync
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
