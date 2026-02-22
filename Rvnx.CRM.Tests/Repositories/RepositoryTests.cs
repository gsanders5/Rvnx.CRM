using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;

namespace Rvnx.CRM.Tests.Repositories
{
    public class RepositoryTests
    {
        private static CRMDbContext GetInMemoryDbContext()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Mock<ICurrentUserService> mockUserService = new();
            mockUserService.Setup(u => u.UserId).Returns((Guid?) null);
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

            // Create Employer
            Employer employer = new()
            {
                Id = Guid.NewGuid(),
                CompanyName = "Acme Corp",
                EmployeeId = contactId
            };
            await repo.AddAsync(employer);
            await repo.SaveChangesAsync();

            // Act
            List<Contact> result = await repo.ListAsNoTrackingAsync<Contact>(c => c.LastName == "Doe", default, "Employers");

            // Assert
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
    }
}
