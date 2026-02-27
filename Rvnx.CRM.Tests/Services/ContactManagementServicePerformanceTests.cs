using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services
{
    public class ContactManagementServicePerformanceTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<IFileValidationService> _fileValidationServiceMock;
        private readonly ContactManagementService _service;

        // Counters for calls
        private int _relationshipListCalls;
        private int _contactListCalls;

        public ContactManagementServicePerformanceTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _fileValidationServiceMock = new Mock<IFileValidationService>();
            _service = new ContactManagementService(_repositoryMock.Object, _fileValidationServiceMock.Object);
        }

        [Fact]
        public async Task DeleteContactAsyncWithManyPartialContactsShouldOptimizeQueries()
        {
            // Arrange
            int partialContactCount = 10;
            Guid contactId = Guid.NewGuid();

            // Generate Partial Contacts
            List<Contact> partialContacts = Enumerable.Range(0, partialContactCount)
                .Select(i => new Contact { Id = Guid.NewGuid(), IsPartial = true })
                .ToList();

            // Generate Sibling Relationships (stable IDs)
            // Each partial contact is related to a "Full" contact (sibling), ensuring they are NOT orphans.
            List<Relationship> siblingRelationships = partialContacts.Select(p => new Relationship
            {
                EntityId = p.Id,
                RelatedEntityId = Guid.NewGuid(), // Unique sibling
                EntityType = EntityTypes.Person
            }).ToList();

            // Initial Relationships (Deleted contact <-> Partial contacts)
            List<Relationship> initialRelationships = partialContacts.Select(p => new Relationship
            {
                EntityId = contactId,
                RelatedEntityId = p.Id,
                EntityType = EntityTypes.Person
            }).ToList();

            // Prepare Memory Data
            List<Relationship> allRelationships = [.. initialRelationships, .. siblingRelationships];

            List<Contact> allContacts =
            [
                .. partialContacts,
                // Add Siblings as Full Contacts
                .. siblingRelationships.Select(r => new Contact { Id = r.RelatedEntityId, IsPartial = false }),
            ];


            // Setup 1: Get Users (return empty)
            _repositoryMock.Setup(r => r.ListAsync<Rvnx.CRM.Core.Models.User>(It.IsAny<Expression<Func<Rvnx.CRM.Core.Models.User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Setup 2: ListAsync<Relationship> (2-arg)
            _repositoryMock.Setup(r => r.ListAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<Relationship, bool>> predicate, CancellationToken ct) =>
                {
                    _relationshipListCalls++;
                    Func<Relationship, bool> func = predicate.Compile();
                    return allRelationships.Where(func).ToList();
                });

            // Setup 3: ListAsync<Relationship> (3-arg)
            _repositoryMock.Setup(r => r.ListAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync((Expression<Func<Relationship, bool>> predicate, CancellationToken ct, string[] includes) =>
                {
                    _relationshipListCalls++;
                    Func<Relationship, bool> func = predicate.Compile();
                    return allRelationships.Where(func).ToList();
                });

            // Setup 4: ListAsync<Contact> (3-arg) - used by ListByChunkedContainsAsync
            _repositoryMock.Setup(r => r.ListAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                 .ReturnsAsync((Expression<Func<Contact, bool>> predicate, CancellationToken ct, string[] includes) =>
                 {
                     _contactListCalls++;
                     Func<Contact, bool> func = predicate.Compile();
                     return allContacts.Where(func).ToList();
                 });

            // Setup 5: ListAsNoTrackingAsync<Contact> (3-arg) - used by ListByChunkedContainsAsync when asNoTracking=true
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                 .ReturnsAsync((Expression<Func<Contact, bool>> predicate, CancellationToken ct, string[] includes) =>
                 {
                     _contactListCalls++;
                     Func<Contact, bool> func = predicate.Compile();
                     return allContacts.Where(func).ToList();
                 });

            // Also mock dependencies deletion calls to avoid null ref if we accidentally delete something
            _repositoryMock.Setup(r => r.ListAsync<Pet>(It.IsAny<Expression<Func<Pet, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsync<Fact>(It.IsAny<Expression<Func<Fact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsync<Note>(It.IsAny<Expression<Func<Note, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);


            // Act
            await _service.DeleteContactAsync(contactId);

            // Assert
            // Optimized implementation:
            // 1. Initial Relationship fetch (1 call, 2-arg)
            // 2. Partial contacts fetch (1 call, Contact 3-arg)
            // 3. Batch Relationship fetch (1 call, 3-arg)
            // 4. Batch Sibling fetch (1 call, Contact 3-arg)

            // Total Relationship Calls = 1 + 1 = 2.
            // But we also have calls in DeleteContactDependenciesAsync(contactId) at the start.
            // DeleteContactDependenciesAsync(contactId):
            //    - DeleteRelatedEntitiesAsync -> ListAsync<Relationship> (2-arg) -> +1
            //    - relatedTo -> ListAsync<Relationship> (2-arg) -> +1

            // So Total Relationship Calls = 1 (initial) + 2 (delete deps) + 1 (bulk partial rels) = 4.

            // Total Contact Calls = 1 (partial contacts) + 1 (siblings) = 2.

            // If fallback deletion happened (bug), we'd have 20 more relationship calls.

            Assert.True(_relationshipListCalls <= 4, $"Expected <= 4 relationship calls, but got {_relationshipListCalls}");
            Assert.True(_contactListCalls <= 2, $"Expected <= 2 contact calls, but got {_contactListCalls}");
        }

        [Fact]
        public async Task CreateContactAsyncUsesSingleSaveChangesAsync()
        {
            // Arrange
            ContactFormDto dto = new()
            {
                FirstName = "Performance",
                LastName = "Test",
                Email = "perf@example.com",
                Phone = "123456789",
                Birthday = new DateTime(1990, 1, 1)
            };

            // Act
            await _service.CreateContactAsync(dto);

            // Assert
            // Verifies that only ONE SaveChangesAsync is called, reducing DB roundtrips
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ContactMethod>(), It.IsAny<CancellationToken>()), Times.Exactly(2)); // Email and Phone
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()), Times.Once); // Birthday
        }
    }
}
