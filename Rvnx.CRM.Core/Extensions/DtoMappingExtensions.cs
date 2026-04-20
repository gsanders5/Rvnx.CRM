using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Models.Activity;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;

namespace Rvnx.CRM.Core.Extensions;

public static class DtoMappingExtensions
{
    public static NoteDto ToDto(this Note entity)
    {
        return new NoteDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Value = entity.Value,
            CreatedDate = entity.CreatedDate,
            EntityId = entity.ContactId ?? Guid.Empty,
            EntityType = EntityType.Person
        };
    }

    public static SignificantDateDto ToDto(this SignificantDate entity)
    {
        return new SignificantDateDto
        {
            Id = entity.Id,
            Title = entity.Title ?? string.Empty,
            EventDate = entity.EventDate,
            Description = entity.Description,
            EntityId = entity.ContactId ?? Guid.Empty,
            EntityType = EntityType.Person,
            RecurrenceType = entity.RecurrenceType,
            CustomIntervalDays = entity.CustomIntervalDays,
            IsActive = entity.IsActive,
            NextOccurrence = entity.GetNextOccurrence(),
            ReminderOffsets = entity.ReminderOffsets?.Select(ro => new ReminderOffsetDto
            {
                Id = ro.Id,
                DaysBeforeEvent = ro.DaysBeforeEvent,
                IsActive = ro.IsActive,
                ScheduledFor =
                    Services.DateCalculationService.GetScheduledForDate(entity, ro,
                        DateOnly.FromDateTime(DateTime.Today))
            }).ToList() ?? []
        };
    }

    public static RelationshipDto ToDto(this Relationship entity)
    {
        RelationshipTypeDefinition? def = RelationshipTypeService.GetById(entity.RelationshipTypeId);
        string typeName = def?.GetName((entity.Person as Contact)?.Gender) ?? entity.RelationshipTypeName;
        string oppositeName = def?.GetOppositeName((entity.RelatedPerson as Contact)?.Gender) ??
                              entity.RelationshipTypeOppositeName;

        return new RelationshipDto
        {
            Id = entity.Id,
            EntityId = entity.EntityId,
            EntityType = entity.EntityType,
            RelatedEntityId = entity.RelatedEntityId,
            RelationshipTypeId = entity.RelationshipTypeId,
            RelationshipTypeName = typeName,
            RelationshipTypeOppositeName = oppositeName,
            RelationshipTypeCategory = def?.Category ?? "Uncategorized",
            RelatedEntityName = entity.RelatedPerson?.FullName ?? "Unknown",
            EntityName = entity.Person?.FullName ?? "Unknown",
            IsEntityPartial = (entity.Person as Contact)?.IsPartial == true,
            IsRelatedEntityPartial = (entity.RelatedPerson as Contact)?.IsPartial == true,
            Description = entity.Description,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }

    public static AttachmentDto ToDto(this Attachment entity)
    {
        return new AttachmentDto
        {
            Id = entity.Id,
            FileName = entity.FileName ?? string.Empty,
            ContentType = entity.ContentType,
            AttachmentType = entity.AttachmentType,
            EntityId = entity.ContactId ?? Guid.Empty,
            EntityType = EntityType.Person
        };
    }

    public static PetDto ToDto(this Pet entity)
    {
        List<Guid> contactIds = entity.PetContacts?.Select(pc => pc.ContactId).ToList() ?? [];
        return new PetDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Species = entity.Species,
            Breed = entity.Breed,
            Birthday = entity.Birthday,
            Notes = entity.Notes,
            ContactIds = contactIds,
            EntityId = contactIds.FirstOrDefault()
        };
    }

    public static ContactMethodDto ToDto(this ContactMethod entity)
    {
        return new ContactMethodDto
        {
            Id = entity.Id,
            Type = entity.Type,
            Value = entity.Value,
            Label = entity.Label,
            EntityId = entity.ContactId ?? Guid.Empty,
            EntityType = EntityType.Person,
            CreatedDate = entity.CreatedDate
        };
    }

    public static FactDto ToDto(this Fact entity)
    {
        return new FactDto
        {
            Id = entity.Id,
            Category = entity.Category,
            Value = entity.Value,
            EntityId = entity.ContactId ?? Guid.Empty,
            EntityType = EntityType.Person,
            CreatedDate = entity.CreatedDate
        };
    }

    public static ContactDto ToDto(this Contact entity)
    {
        return new ContactDto
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName ?? string.Empty,
            MaidenName = entity.MaidenName,
            FullName = entity.FullName,
            Company = entity.Company,
            JobTitle = entity.JobTitle,
            IsHidden = entity.IsHidden,
            CreatedDate = entity.CreatedDate,
            LastChangedDate = entity.LastChangedDate,
            CreatedBy = entity.CreatedBy,
            LastChangedBy = entity.LastChangedBy,
            UserId = entity.UserId?.ToString(),
            ProfileImageId = entity.ProfileImageId,
            Pronouns = entity.Pronouns,
            Gender = entity.Gender,
            Religion = entity.Religion,
            IsPartial = entity.IsPartial
        };
    }

    public static ContactDetailDto ToDetailDto(this Contact entity)
    {
        return new ContactDetailDto
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName ?? string.Empty,
            MaidenName = entity.MaidenName,
            FullName = entity.FullName,
            Company = entity.Company,
            JobTitle = entity.JobTitle,
            Nickname = entity.Nickname,
            ProfileImageId = entity.ProfileImageId,
            Pronouns = entity.Pronouns,
            Gender = entity.Gender,
            Religion = entity.Religion,

            // Lists will be populated separately or via mapping if loaded
            Notes = entity.Notes?.Select(n => n.ToDto()) ?? [],
            SignificantDates = entity.SignificantDates?.Select(d => d.ToDto()) ?? [],
            Relationships = entity.Relationships?.Select(r => r.ToDto()) ?? [],
            RelatedTo = entity.RelatedTo?.Select(r => r.ToDto()) ?? [],
            ContactMethods = entity.ContactMethods?.Select(i => i.ToDto()) ?? [],
            Facts = entity.Facts?.Select(f => f.ToDto()) ?? [],
            Attachments = entity.Attachments?.Select(a => a.ToDto()) ?? [],
            Addresses = entity.Addresses?.Select(a => a.ToDto()) ?? [],
            ContactTasks = entity.ContactTasks?.Select(t => t.ToDto()) ?? [],
            // Pets to be populated by caller as they are not on Person
        };
    }

    public static Contact ToEntity(this ContactFormDto dto)
    {
        return new Contact
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            MaidenName = dto.MaidenName,
            Nickname = dto.Nickname,
            JobTitle = dto.JobTitle,
            Company = dto.Company,
            IsHidden = dto.IsHidden,
            Pronouns = dto.Pronouns,
            Gender = dto.Gender,
            Religion = dto.Religion
        };
    }

    public static void UpdateEntity(this Contact entity, ContactFormDto dto)
    {
        entity.FirstName = dto.FirstName;
        entity.LastName = dto.LastName;
        entity.MaidenName = dto.MaidenName;
        entity.Nickname = dto.Nickname;
        entity.JobTitle = dto.JobTitle;
        entity.Company = dto.Company;
        entity.IsHidden = dto.IsHidden;
        entity.Pronouns = dto.Pronouns;
        entity.Gender = dto.Gender;
        entity.Religion = dto.Religion;
    }

    public static Pet ToEntity(this PetFormDto dto)
    {
        return new Pet
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Species = dto.Species,
            Breed = dto.Breed,
            Birthday = dto.Birthday,
            Notes = dto.Notes
        };
    }

    public static void UpdateEntity(this Pet entity, PetFormDto dto)
    {
        entity.Name = dto.Name;
        entity.Species = dto.Species;
        entity.Breed = dto.Breed;
        entity.Birthday = dto.Birthday;
        entity.Notes = dto.Notes;
    }

    public static ContactMethod ToEntity(this ContactMethodFormDto dto)
    {
        return new ContactMethod
        {
            Id = Guid.NewGuid(),
            Type = dto.Type,
            Value = dto.Value,
            Label = dto.Label,
            ContactId = dto.EntityId
        };
    }

    public static void UpdateEntity(this ContactMethod entity, ContactMethodFormDto dto)
    {
        entity.Type = dto.Type;
        entity.Value = dto.Value;
        entity.Label = dto.Label;
    }

    public static Note ToEntity(this NoteFormDto dto)
    {
        return new Note { Id = Guid.NewGuid(), Title = dto.Title, Value = dto.Value, ContactId = dto.EntityId };
    }

    public static void UpdateEntity(this Note entity, NoteFormDto dto)
    {
        entity.Title = dto.Title;
        entity.Value = dto.Value;
    }

    public static Fact ToEntity(this FactFormDto dto)
    {
        return new Fact
        {
            Id = Guid.NewGuid(),
            Category = dto.Category,
            Value = dto.Value,
            ContactId = dto.EntityId
        };
    }

    public static void UpdateEntity(this Fact entity, FactFormDto dto)
    {
        entity.Category = dto.Category;
        entity.Value = dto.Value;
    }

    public static Relationship ToEntity(this RelationshipFormDto dto)
    {
        return new Relationship
        {
            Id = Guid.NewGuid(),
            EntityId = dto.EntityId,
            RelatedEntityId = dto.RelatedEntityId,
            EntityType = dto.EntityType,
            RelationshipTypeId = dto.RelationshipTypeId,
            Description = dto.Description,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };
    }

    public static void UpdateEntity(this Relationship entity, RelationshipFormDto dto)
    {
        entity.EntityId = dto.EntityId;
        entity.RelatedEntityId = dto.RelatedEntityId;
        entity.RelationshipTypeId = dto.RelationshipTypeId;
        entity.Description = dto.Description;
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;
    }

    public static ActivityDto ToDto(this Activity entity)
    {
        List<Guid> contactIds = entity.ActivityContacts?.Select(ac => ac.ContactId).ToList() ?? [];
        return new ActivityDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            ActivityDate = entity.ActivityDate,
            ActivityType = entity.ActivityType,
            Location = entity.Location,
            EntityId = contactIds.FirstOrDefault(),
            ContactIds = contactIds,
            CreatedDate = entity.CreatedDate,
            CreatedBy = entity.CreatedBy,
            LastChangedDate = entity.LastChangedDate,
            LastChangedBy = entity.LastChangedBy
        };
    }

    public static Activity ToEntity(this ActivityFormDto dto)
    {
        return new Activity
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            ActivityDate = dto.ActivityDate,
            ActivityType = dto.ActivityType,
            Location = dto.Location
        };
    }

    public static void UpdateEntity(this Activity entity, ActivityFormDto dto)
    {
        entity.Title = dto.Title;
        entity.Description = dto.Description;
        entity.ActivityDate = dto.ActivityDate;
        entity.ActivityType = dto.ActivityType;
        entity.Location = dto.Location;
    }

    public static AddressDto ToDto(this Address entity)
    {
        return new AddressDto
        {
            Id = entity.Id,
            Line1 = entity.Line1,
            Line2 = entity.Line2,
            City = entity.City,
            State = entity.State,
            Zip = entity.Zip,
            Country = entity.Country,
            AddressType = entity.AddressType,
            EntityId = entity.ContactId ?? Guid.Empty,
            CreatedDate = entity.CreatedDate,
            CreatedBy = entity.CreatedBy,
            LastChangedDate = entity.LastChangedDate,
            LastChangedBy = entity.LastChangedBy
        };
    }

    public static Address ToEntity(this AddressFormDto dto)
    {
        return new Address
        {
            Id = Guid.NewGuid(),
            Line1 = dto.Line1,
            Line2 = dto.Line2,
            City = dto.City,
            State = dto.State,
            Zip = dto.Zip,
            Country = dto.Country,
            AddressType = dto.AddressType,
            ContactId = dto.EntityId
        };
    }

    public static void UpdateEntity(this Address entity, AddressFormDto dto)
    {
        entity.Line1 = dto.Line1;
        entity.Line2 = dto.Line2;
        entity.City = dto.City;
        entity.State = dto.State;
        entity.Zip = dto.Zip;
        entity.Country = dto.Country;
        entity.AddressType = dto.AddressType;
    }

    public static ContactTaskDto ToDto(this ContactTask entity)
    {
        return new ContactTaskDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            DueDate = entity.DueDate,
            IsCompleted = entity.IsCompleted,
            CompletedDate = entity.CompletedDate,
            EntityId = entity.ContactId ?? Guid.Empty,
            CreatedDate = entity.CreatedDate,
            CreatedBy = entity.CreatedBy,
            LastChangedDate = entity.LastChangedDate,
            LastChangedBy = entity.LastChangedBy
        };
    }

    public static ContactTask ToEntity(this ContactTaskFormDto dto)
    {
        return new ContactTask
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            DueDate = dto.DueDate,
            IsCompleted = dto.IsCompleted,
            ContactId = dto.EntityId
        };
    }

    public static void UpdateEntity(this ContactTask entity, ContactTaskFormDto dto)
    {
        entity.Title = dto.Title;
        entity.Description = dto.Description;
        entity.DueDate = dto.DueDate;
        entity.IsCompleted = dto.IsCompleted;
    }
}