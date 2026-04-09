using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Tests.Extensions;

public class PetDtoMappingTests
{
    [Fact]
    public void ToDtoShouldMapPropertiesCorrectly()
    {
        Guid contactId1 = Guid.NewGuid();
        Guid contactId2 = Guid.NewGuid();
        Pet entity = new()
        {
            Id = Guid.NewGuid(),
            Name = "Buddy",
            Species = "Dog",
            Breed = "Golden Retriever",
            Birthday = new DateTime(2020, 1, 1),
            Notes = "Loves tennis balls",
            PetContacts =
            [
                new PetContact { ContactId = contactId1 },
                new PetContact { ContactId = contactId2 }
            ]
        };

        PetDto dto = entity.ToDto();

        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.Name, dto.Name);
        Assert.Equal(entity.Species, dto.Species);
        Assert.Equal(entity.Breed, dto.Breed);
        Assert.Equal(entity.Birthday, dto.Birthday);
        Assert.Equal(entity.Notes, dto.Notes);
        Assert.Equal(contactId1, dto.EntityId);
        Assert.Equal(2, dto.ContactIds.Count);
        Assert.Contains(contactId1, dto.ContactIds);
        Assert.Contains(contactId2, dto.ContactIds);
    }

    [Fact]
    public void ToDtoWhenNoPetContactsShouldReturnEmptyContactIds()
    {
        Pet entity = new()
        {
            Id = Guid.NewGuid(),
            Name = "Buddy",
            PetContacts = []
        };

        PetDto dto = entity.ToDto();

        Assert.Empty(dto.ContactIds);
        Assert.Equal(Guid.Empty, dto.EntityId);
    }

    [Fact]
    public void ToEntityShouldCreateNewPetWithCorrectProperties()
    {
        PetFormDto dto = new()
        {
            Name = "Mittens",
            Species = "Cat",
            Breed = "Siamese",
            Birthday = new DateTime(2019, 5, 15),
            Notes = "Hates water",
            EntityId = Guid.NewGuid(),
            ContactIds = [Guid.NewGuid(), Guid.NewGuid()]
        };

        Pet entity = dto.ToEntity();

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal(dto.Name, entity.Name);
        Assert.Equal(dto.Species, entity.Species);
        Assert.Equal(dto.Breed, entity.Breed);
        Assert.Equal(dto.Birthday, entity.Birthday);
        Assert.Equal(dto.Notes, entity.Notes);
    }

    [Fact]
    public void UpdateEntityShouldUpdatePropertiesCorrectly()
    {
        Pet entity = new()
        {
            Id = Guid.NewGuid(),
            Name = "Old Name",
            Species = "Old Species",
            Breed = "Old Breed",
            Birthday = DateTime.MinValue,
            Notes = "Old Notes"
        };

        PetFormDto dto = new()
        {
            Name = "New Name",
            Species = "New Species",
            Breed = "New Breed",
            Birthday = DateTime.UtcNow,
            Notes = "New Notes",
            EntityId = Guid.NewGuid()
        };

        entity.UpdateEntity(dto);

        Assert.Equal(dto.Name, entity.Name);
        Assert.Equal(dto.Species, entity.Species);
        Assert.Equal(dto.Breed, entity.Breed);
        Assert.Equal(dto.Birthday, entity.Birthday);
        Assert.Equal(dto.Notes, entity.Notes);
    }
}