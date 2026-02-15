using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Repositories;
using Xunit;

namespace Rvnx.CRM.Tests.Integration;

public class RepositoryIntegrationTests : SqliteIntegrationTestBase
{
    private readonly Repository _repository;

    public RepositoryIntegrationTests() : base("TestUser")
    {
        _repository = new Repository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistData_ToRealSqliteDB()
    {
        // Arrange
        var contact = new Contact { FirstName = "Real", LastName = "Database" };

        // Act
        await _repository.AddAsync(contact);
        await _repository.SaveChangesAsync();

        // Assert
        // Use a separate context/repo to verify persistence to disk
        // We can't easily create a new context pointing to the same file in the helper without exposing connection string,
        // but we can query _context directly if we clear change tracker or detach.
        _context.ChangeTracker.Clear();

        var retrieved = await _context.Contacts.FirstOrDefaultAsync(c => c.FirstName == "Real");
        retrieved.Should().NotBeNull();
        retrieved!.LastName.Should().Be("Database");
        retrieved.UserId.Should().Be("TestUser"); // Audit field check
    }

    [Fact]
    public async Task GlobalQueryFilter_ShouldFilterData_OnRealDB()
    {
        // 1. Add data as "UserA" (Current Context is TestUser)
        // We will mock TestUser as "UserA" for this test by updating the existing mock setup if possible?
        // No, constructor set it. Let's assume TestUser = UserA.

        var myContact = new Contact { FirstName = "My", LastName = "Contact" };
        await _repository.AddAsync(myContact);
        await _repository.SaveChangesAsync();

        // 2. Add data as "UserB" directly via SQL or by temporarily changing User ID behavior?
        // Easier: Execute Raw SQL to insert a record with different UserId, bypassing filter for insertion.
        // Or create a separate context pointed at the same DB file.
        // Since SqliteIntegrationTestBase generates a random file, we need that path to share it.
        // Let's just use Raw SQL to seed "Other" data.

        var otherId = Guid.NewGuid();
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        // Note: SQLite syntax - Table name is Contact, not Contacts
        await _context.Database.ExecuteSqlRawAsync(
            "INSERT INTO Contact (Id, FirstName, LastName, IsHidden, CreatedBy, CreatedDate, LastChangedBy, LastChangedDate, UserId) " +
            "VALUES ({0}, 'Other', 'Guy', 0, 'System', {1}, 'System', {1}, 'UserB')",
            otherId, now);

        // 3. Act
        _context.ChangeTracker.Clear();
        var visibleContacts = await _repository.ListAsync<Contact>();

        // 4. Assert
        visibleContacts.Should().Contain(c => c.FirstName == "My");
        visibleContacts.Should().NotContain(c => c.FirstName == "Other");
    }

    [Fact]
    public async Task CascadeDelete_ShouldWork_OnRealDB()
    {
        // Arrange
        var contact = new Contact { FirstName = "Delete", LastName = "Me" };
        await _repository.AddAsync(contact);
        await _repository.SaveChangesAsync();

        // Add related entities
        var date = new SignificantDate
        {
            Id = Guid.NewGuid(),
            EntityId = contact.Id,
            EntityType = EntityTypes.Person,
            Title = "Bday",
            Date = DateTime.Now
        };
        await _repository.AddAsync(date);
        await _repository.SaveChangesAsync();

        // Act
        // Repository delete for generic entities usually requires manual deletion in logic (Controller),
        // but let's see if DB cascade is configured?
        // Our configuration in OnModelCreating doesn't strictly set up DB-level ON DELETE CASCADE for *Polymorphic* relationships
        // because they don't have FK constraints in the DB pointing to the specific Contact table (EntityId is loose).
        // So this test confirms that DB Cascade DOES NOT happen for generic entities, verifying we NEED the manual logic.
        // UNLESS we are testing standard FKs (like Employer -> Contact).

        // Let's test Employer (Standard FK)
        var employer = new Employer { CompanyName = "Work", EmployeeId = contact.Id };
        await _repository.AddAsync(employer);
        await _repository.SaveChangesAsync();

        // Delete Contact
        await _repository.DeleteAsync<Contact>(contact.Id);
        await _repository.SaveChangesAsync();

        // Assert
        _context.ChangeTracker.Clear();

        // Employer should be deleted (Cascade)
        var emp = await _context.Employers.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.CompanyName == "Work");
        emp.Should().BeNull();

        // Generic Entity (SignificantDate) should still exist (No FK)
        var d = await _context.SignificantDates.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Title == "Bday");
        d.Should().NotBeNull();
    }
}
