using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Xunit;

namespace Rvnx.CRM.Tests
{
    public class RepositoryTests
    {
        private CRMDbContext GetInMemoryDbContext()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            CRMDbContext context = new(options);
            return context;
        }

        [Fact]
        public async Task ListAsync_Predicate_ShouldFilterCorrectly()
        {
            using var context = GetInMemoryDbContext();
            var repo = new Repository(context);

            await repo.AddAsync(new Contact { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" });
            await repo.AddAsync(new Contact { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe" });
            await repo.AddAsync(new Contact { Id = Guid.NewGuid(), FirstName = "Bob", LastName = "Smith" });
            await repo.SaveChangesAsync();

            var does = await repo.ListAsync<Contact>(c => c.LastName == "Doe");

            Assert.Equal(2, does.Count);
            Assert.Contains(does, c => c.FirstName == "John");
            Assert.Contains(does, c => c.FirstName == "Jane");
        }

        [Fact]
        public async Task ListAsync_PredicateAndIncludes_ShouldIncludeRelatedEntities()
        {
            using var context = GetInMemoryDbContext();
            var repo = new Repository(context);

            var contactId = Guid.NewGuid();
            var contact = new Contact { Id = contactId, FirstName = "John", LastName = "Doe" };
            await repo.AddAsync(contact);
            await repo.SaveChangesAsync();

            // Create Employer
            var employer = new Employer
            {
                Id = Guid.NewGuid(),
                CompanyName = "Acme Corp",
                EmployeeId = contactId
            };
            await repo.AddAsync(employer);
            await repo.SaveChangesAsync();

            // Act
            var result = await repo.ListAsNoTrackingAsync<Contact>(c => c.LastName == "Doe", default, "Employers");

            // Assert
            var fetchedContact = result.Single();
            Assert.NotNull(fetchedContact.Employers);
            Assert.NotEmpty(fetchedContact.Employers);
            Assert.Equal("Acme Corp", fetchedContact.Employers.First().CompanyName);
        }

        [Fact]
        public async Task CountAsync_Predicate_ShouldCountCorrectly()
        {
            using var context = GetInMemoryDbContext();
            var repo = new Repository(context);

            await repo.AddAsync(new Contact { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" });
            await repo.AddAsync(new Contact { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe" });
            await repo.AddAsync(new Contact { Id = Guid.NewGuid(), FirstName = "Bob", LastName = "Smith" });
            await repo.SaveChangesAsync();

            var count = await repo.CountAsync<Contact>(c => c.LastName == "Doe");

            Assert.Equal(2, count);
        }
    }
}
