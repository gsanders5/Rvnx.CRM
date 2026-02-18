using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.DTOs.Pet;
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
                EntityId = entity.EntityId,
                EntityType = entity.EntityType
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
                EntityId = entity.EntityId,
                EntityType = entity.EntityType,
                RemindMe = entity.RemindMe,
                ReminderSent = entity.ReminderSent,
                EventFrequency = entity.EventFrequency
            };
        }

        public static SignificantDateDto ToDto(this SignificantDate entity)
        {
            return new SignificantDateDto
            {
                Id = entity.Id,
                Title = entity.Title ?? string.Empty,
                Date = entity.Date,
                Description = entity.Description,
                EntityId = entity.EntityId,
                EntityType = entity.EntityType,
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
                EntityId = entity.EntityId,
                EntityType = entity.EntityType
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
                EntityId = entity.EntityId
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
                EntityId = entity.EntityId,
                EntityType = entity.EntityType,
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
                EntityId = entity.EntityId,
                EntityType = entity.EntityType,
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
                ProfileImageId = entity.ProfileImageId
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

                // Lists will be populated separately or via mapping if loaded
                Notes = entity.Notes?.Select(n => n.ToDto()) ?? new List<NoteDto>(),
                Reminders = entity.Reminders?.Select(r => r.ToDto()) ?? new List<ReminderDto>(),
                SignificantDates = entity.SignificantDates?.Select(d => d.ToDto()) ?? new List<SignificantDateDto>(),
                Relationships = entity.Relationships?.Select(r => r.ToDto()) ?? new List<RelationshipDto>(),
                RelatedTo = entity.RelatedTo?.Select(r => r.ToDto()) ?? new List<RelationshipDto>(),
                ContactMethods = entity.ContactMethods?.Select(i => i.ToDto()) ?? new List<ContactMethodDto>(),
                Facts = entity.Facts?.Select(f => f.ToDto()) ?? new List<FactDto>(),
                Attachments = entity.Attachments?.Select(a => a.ToDto()) ?? new List<AttachmentDto>(),
                // Pets to be populated by caller as they are not on Person
            };
        }

        public static Contact ToEntity(this CreateContactDto dto)
        {
            return new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Nickname = dto.Nickname,
                JobTitle = dto.JobTitle,
                Company = dto.Company
                // Email, Phone, Birthday removed from Contact
            };
        }

        public static void UpdateEntity(this Contact entity, UpdateContactDto dto)
        {
            entity.FirstName = dto.FirstName;
            entity.LastName = dto.LastName;
            entity.Nickname = dto.Nickname;
            entity.JobTitle = dto.JobTitle;
            entity.Company = dto.Company;
            // Email, Phone, Birthday removed from Contact
        }

        public static Pet ToEntity(this CreatePetDto dto)
        {
            return new Pet
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Species = dto.Species,
                Breed = dto.Breed,
                Birthday = dto.Birthday,
                Notes = dto.Notes,
                EntityId = dto.EntityId,
                EntityType = EntityTypes.Person
            };
        }

        public static void UpdateEntity(this Pet entity, UpdatePetDto dto)
        {
            entity.Name = dto.Name;
            entity.Species = dto.Species;
            entity.Breed = dto.Breed;
            entity.Birthday = dto.Birthday;
            entity.Notes = dto.Notes;
        }
    }
}
