using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Core.Extensions
{
    public static class DtoMappingExtensions
    {
        // Common Mappings
        public static NoteDto ToDto(this Note entity)
        {
            return new NoteDto
            {
                Id = entity.Id,
                Title = entity.Title,
                Value = entity.Value,
                CreatedDate = entity.CreatedDate,
                EntityId = entity.ContactId ?? Guid.Empty,
                EntityType = EntityTypes.Person
            };
        }

        public static ReminderDto ToDto(this Reminder entity)
        {
            return new ReminderDto
            {
                Id = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
                DueDate = entity.DueDate,
                EntityId = entity.ContactId ?? Guid.Empty,
                EntityType = EntityTypes.Person,
                RemindMe = entity.RemindMe,
                ReminderSent = entity.ReminderSent,
                EventFrequency = entity.EventFrequency
            };
        }

        public static void UpdateEntity(this Reminder entity, ReminderDto dto)
        {
            entity.Title = dto.Title;
            entity.Description = dto.Description;
            entity.DueDate = dto.DueDate;
            entity.IsCompleted = dto.IsCompleted;
            entity.RemindMe = dto.RemindMe;
            entity.EventFrequency = dto.EventFrequency;
        }

        public static SignificantDateDto ToDto(this SignificantDate entity)
        {
            return new SignificantDateDto
            {
                Id = entity.Id,
                Title = entity.Title ?? string.Empty,
                Date = entity.Date,
                Description = entity.Description,
                EntityId = entity.ContactId ?? Guid.Empty,
                EntityType = EntityTypes.Person,
                RemindMe = entity.RemindMe,
                ReminderSent = entity.ReminderSent,
                EventFrequency = entity.EventFrequency
            };
        }

        public static RelationshipDto ToDto(this Relationship entity)
        {
            string typeName = entity.RelationshipTypeName;
            string oppositeName = entity.RelationshipTypeOppositeName;

            return new RelationshipDto
            {
                Id = entity.Id,
                EntityId = entity.EntityId,
                EntityType = entity.EntityType,
                RelatedEntityId = entity.RelatedEntityId,
                RelationshipTypeId = entity.RelationshipTypeId,
                RelationshipTypeName = typeName,
                RelationshipTypeOppositeName = oppositeName,
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
                EntityType = EntityTypes.Person
            };
        }

        public static PetDto ToDto(this Pet entity)
        {
            return new PetDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Species = entity.Species,
                Breed = entity.Breed,
                Birthday = entity.Birthday,
                Notes = entity.Notes,
                EntityId = entity.ContactId
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
                EntityType = EntityTypes.Person,
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
                EntityType = EntityTypes.Person,
                CreatedDate = entity.CreatedDate
            };
        }

        // Contact Mappings
        public static ContactDto ToDto(this Contact entity)
        {
            return new ContactDto
            {
                Id = entity.Id,
                FirstName = entity.FirstName,
                LastName = entity.LastName ?? string.Empty,
                FullName = entity.FullName,
                Company = entity.Company,
                JobTitle = entity.JobTitle,
                CreatedDate = entity.CreatedDate,
                LastChangedDate = entity.LastChangedDate,
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
                Reminders = entity.Reminders?.Select(r => r.ToDto()) ?? [],
                SignificantDates = entity.SignificantDates?.Select(d => d.ToDto()) ?? [],
                Relationships = entity.Relationships?.Select(r => r.ToDto()) ?? [],
                RelatedTo = entity.RelatedTo?.Select(r => r.ToDto()) ?? [],
                ContactMethods = entity.ContactMethods?.Select(i => i.ToDto()) ?? [],
                Facts = entity.Facts?.Select(f => f.ToDto()) ?? [],
                Attachments = entity.Attachments?.Select(a => a.ToDto()) ?? [],
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
                Notes = dto.Notes,
                ContactId = dto.EntityId
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
            // ContactId should not be changeable
        }
        public static Note ToEntity(this NoteFormDto dto)
        {
            return new Note
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Value = dto.Value,
                ContactId = dto.EntityId
            };
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
    }
}
