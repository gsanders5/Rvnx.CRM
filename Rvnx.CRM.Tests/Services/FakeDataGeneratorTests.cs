using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using Rvnx.CRM.Infrastructure.Services;

namespace Rvnx.CRM.Tests.Services;

public class FakeDataGeneratorTests
{
    [Fact]
    public void GenerateContactsSetsGenderAndPronounsFromConstants()
    {
        // Arrange
        int count = 20; // Increased count to ensure better chance of hitting both genders

        // Act
        List<Contact> contacts = FakeDataGenerator.GenerateContacts(count);

        // Assert
        Assert.Equal(count, contacts.Count);
        foreach (Contact contact in contacts)
        {
            Assert.NotNull(contact.Gender);
            Assert.NotNull(contact.Pronouns);

            // Validate that the generated values exist in the PersonalAttributeOptions lists
            Assert.Contains(contact.Gender, PersonalAttributeOptions.Gender);
            Assert.Contains(contact.Pronouns, PersonalAttributeOptions.Pronouns);
        }
    }

    [Fact]
    public void GenerateRelationshipsCreatesValidRelationships()
    {
        // Arrange
        int contactCount = 10;
        int relationshipCount = 5;
        List<Contact> contacts = FakeDataGenerator.GenerateContacts(contactCount);

        // Act
        List<Relationship> relationships = FakeDataGenerator.GenerateRelationships(contacts, relationshipCount);

        // Assert
        Assert.Equal(relationshipCount, relationships.Count);
        HashSet<Guid> validTypes = RelationshipTypeService.GetByEntityType(EntityTypes.Person).Select(t => t.Id).ToHashSet();

        foreach (Relationship rel in relationships)
        {
            Assert.Equal(EntityTypes.Person, rel.EntityType);
            Assert.Contains(rel.RelationshipTypeId, validTypes);
            Assert.NotEqual(rel.EntityId, rel.RelatedEntityId);
            Assert.Contains(rel.EntityId, contacts.Select(c => c.Id));
            Assert.Contains(rel.RelatedEntityId, contacts.Select(c => c.Id));
        }
    }
}
