using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Repositories;
using System.Globalization;

namespace Rvnx.CRM.Tests.Integration;

public class RepositoryIntegrationTests : SqliteIntegrationTestBase
{
    private static readonly Guid TestUserId = Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938");

    private readonly Repository _repository;

    public RepositoryIntegrationTests() : base(TestUserId)
    {
        _repository = new Repository(Context);
    }

    [Fact]
    public async Task AddAsyncShouldPersistDataToRealSqliteDB()
    {
        // Arrange
        Contact contact = new() { FirstName = "Real", LastName = "Database" };

        // Act
        await _repository.AddAsync(contact);
        await _repository.SaveChangesAsync();

        // Assert
        // Use a separate context/repo to verify persistence to disk
        // We can't easily create a new context pointing to the same file in the helper without exposing connection string,
        // but we can query Context directly if we clear change tracker or detach.
        Context.ChangeTracker.Clear();

        Contact? retrieved = await Context.Contacts.FirstOrDefaultAsync(c => c.FirstName == "Real");
        Assert.NotNull(retrieved);
        Assert.Equal("Database", retrieved!.LastName);
        Assert.Equal(TestUserId, retrieved.UserId); // Audit field check
    }

    [Fact]
    public async Task GlobalQueryFilterShouldFilterDataOnRealDB()
    {
        // 1. Add data as "UserA" (Current Context is TestUser)
        // We will mock TestUser as "UserA" for this test by updating the existing mock setup if possible?
        // No, constructor set it. Let's assume TestUser = UserA.

        Contact myContact = new() { FirstName = "My", LastName = "Contact" };
        await _repository.AddAsync(myContact);
        await _repository.SaveChangesAsync();

        // 2. Add data as "UserB" directly via SQL or by temporarily changing User ID behavior?
        // Easier: Execute Raw SQL to insert a record with different UserId, bypassing filter for insertion.
        // Or create a separate context pointed at the same DB file.
        // Since SqliteIntegrationTestBase generates a random file, we need that path to share it.
        // Let's just use Raw SQL to seed "Other" data.

        Guid otherId = Guid.NewGuid();
        string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        // Note: SQLite syntax - Table name is Contact, not Contacts
        await Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO Contact (Id, FirstName, LastName, IsHidden, CreatedBy, CreatedDate, LastChangedBy, LastChangedDate, UserId, IsPartial) " +
            "VALUES ({0}, 'Other', 'Guy', 0, 'System', {1}, 'System', {1}, 'UserB', 0)",
            otherId, now);

        // 3. Act
        Context.ChangeTracker.Clear();
        List<Contact> visibleContacts = await _repository.ListAsync<Contact>();

        // 4. Assert
        Assert.Contains(visibleContacts, c => c.FirstName == "My");
        Assert.DoesNotContain(visibleContacts, c => c.FirstName == "Other");
    }

    [Fact]
    public async Task CascadeDeleteShouldWorkOnRealDB()
    {
        // Arrange
        Contact contact = new() { FirstName = "Delete", LastName = "Me" };
        await _repository.AddAsync(contact);
        await _repository.SaveChangesAsync();

        // Add related entities
        SignificantDate date = new()
        {
            Id = Guid.NewGuid(),
            ContactId = contact.Id,
            Title = "Bday",
            Date = DateTime.Now
        };
        await _repository.AddAsync(date);
        await _repository.SaveChangesAsync();

        // Let's test Employer (Standard FK)
        Employer employer = new() { CompanyName = "Work", EmployeeId = contact.Id };
        await _repository.AddAsync(employer);
        await _repository.SaveChangesAsync();

        // Act
        // Delete Contact
        await _repository.DeleteAsync<Contact>(contact.Id);
        await _repository.SaveChangesAsync();

        // Assert
        Context.ChangeTracker.Clear();

        // Employer should be deleted (Cascade)
        Employer? emp = await Context.Employers.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.CompanyName == "Work");
        Assert.Null(emp);

        // SignificantDate should now be deleted because of the new explicit FK with Cascade Delete
        SignificantDate? d = await Context.SignificantDates.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Title == "Bday");
        Assert.Null(d);
    }
}
