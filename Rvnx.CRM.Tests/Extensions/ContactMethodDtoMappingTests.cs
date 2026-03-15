using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Tests.Extensions;

public class ContactMethodDtoMappingTests
{
    [Fact]
    public void ToDtoShouldMapPropertiesCorrectly()
    {
        ContactMethod entity = new()
        {
            Id = Guid.NewGuid(),
            Type = ContactMethodType.Email,
            Value = "test@example.com",
            Label = "Work",
            ContactId = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow
        };

        ContactMethodDto dto = entity.ToDto();

        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.Type, dto.Type);
        Assert.Equal(entity.Value, dto.Value);
        Assert.Equal(entity.Label, dto.Label);
        Assert.Equal(entity.ContactId.Value, dto.EntityId);
        Assert.Equal(EntityTypes.Person, dto.EntityType);
        Assert.Equal(entity.CreatedDate, dto.CreatedDate);
    }

    [Fact]
    public void ToDtoShouldHandleNullContactId()
    {
        ContactMethod entity = new()
        {
            Id = Guid.NewGuid(),
            Type = ContactMethodType.Phone,
            Value = "+1987654321",
            Label = "Mobile",
            ContactId = null,
            CreatedDate = DateTime.UtcNow
        };

        ContactMethodDto dto = entity.ToDto();

        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.Type, dto.Type);
        Assert.Equal(entity.Value, dto.Value);
        Assert.Equal(entity.Label, dto.Label);
        Assert.Equal(Guid.Empty, dto.EntityId);
        Assert.Equal(EntityTypes.Person, dto.EntityType);
        Assert.Equal(entity.CreatedDate, dto.CreatedDate);
    }

    [Fact]
    public void ToEntityShouldCreateNewContactMethodWithCorrectProperties()
    {
        ContactMethodFormDto dto = new()
        {
            Type = ContactMethodType.Phone,
            Value = "+1234567890",
            Label = "Mobile",
            EntityId = Guid.NewGuid()
        };

        ContactMethod entity = dto.ToEntity();

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal(dto.Type, entity.Type);
        Assert.Equal(dto.Value, entity.Value);
        Assert.Equal(dto.Label, entity.Label);
        Assert.Equal(dto.EntityId, entity.ContactId);
    }

    [Fact]
    public void UpdateEntityShouldUpdatePropertiesCorrectly()
    {
        Guid initialContactId = Guid.NewGuid();
        ContactMethod entity = new()
        {
            Id = Guid.NewGuid(),
            Type = ContactMethodType.Email,
            Value = "old@example.com",
            Label = "Old Label",
            ContactId = initialContactId
        };

        ContactMethodFormDto dto = new()
        {
            Type = ContactMethodType.Website,
            Value = "https://example.com",
            Label = "New Label",
            // EntityId in DTO might be different but should be ignored by UpdateEntity
            EntityId = Guid.NewGuid()
        };

        entity.UpdateEntity(dto);

        Assert.Equal(dto.Type, entity.Type);
        Assert.Equal(dto.Value, entity.Value);
        Assert.Equal(dto.Label, entity.Label);

        Assert.Equal(initialContactId, entity.ContactId);
    }

    [Fact]
    public void UpdateEntityShouldPreserveIdAndUpdateValues()
    {
        Guid initialId = Guid.NewGuid();
        ContactMethod entity = new()
        {
            Id = initialId,
            Type = ContactMethodType.Email,
            Value = "test@example.com",
            Label = "Work"
        };

        ContactMethodFormDto dto = new()
        {
            // DTO might have a different ID or none, but Entity ID should never change
            Id = Guid.NewGuid(),
            Type = ContactMethodType.Phone,
            Value = "123",
            Label = "Mobile"
        };

        entity.UpdateEntity(dto);

        Assert.Equal(initialId, entity.Id);

        Assert.Equal(dto.Type, entity.Type);
        Assert.Equal(dto.Value, entity.Value);
        Assert.Equal(dto.Label, entity.Label);
    }

    [Fact]
    public void UpdateEntityShouldHandleNullLabel()
    {
        ContactMethod entity = new()
        {
            Type = ContactMethodType.Email,
            Value = "test@example.com",
            Label = "Work"
        };

        ContactMethodFormDto dto = new()
        {
            Type = ContactMethodType.Email,
            Value = "test@example.com",
            Label = null
        };

        entity.UpdateEntity(dto);

        Assert.Null(entity.Label);
    }
}