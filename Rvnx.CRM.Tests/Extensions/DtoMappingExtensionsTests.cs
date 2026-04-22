using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using System.Reflection;

namespace Rvnx.CRM.Tests.Extensions;

public class DtoMappingExtensionsTests
{
    public class GeneralTests
    {
        [Fact]
        public void ToDtoShouldMapLastChangedDateFromContact()
        {
            DateTime lastChangedDate = new(2023, 10, 27, 12, 0, 0, DateTimeKind.Utc);
            Contact contact = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "Contact",
                LastChangedDate = lastChangedDate
            };

            ContactDto dto = contact.ToDto();

            Assert.Equal(lastChangedDate, dto.LastChangedDate);
            Assert.NotEqual(DateTime.MinValue, dto.LastChangedDate);
        }

        [Fact]
        public void ToDtoShouldMapAllMatchingProperties()
        {
            Contact contact = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "TestFirst",
                LastName = "TestLast",
                Company = "TestCompany",
                JobTitle = "TestJob",
                IsHidden = true,
                ProfileImageId = Guid.NewGuid(),
                Pronouns = "They/Them",
                Gender = "Non-binary",
                Religion = "Agnostic",
                IsPartial = true,
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                LastChangedDate = DateTime.UtcNow,
                CreatedBy = "TestCreator",
                LastChangedBy = "TestChanger",
                UserId = Guid.NewGuid()
            };

            ContactDto dto = contact.ToDto();

            PropertyInfo[] entityProps = typeof(Contact).GetProperties();
            PropertyInfo[] dtoProps = typeof(ContactDto).GetProperties();

            foreach (PropertyInfo dtoProp in dtoProps)
            {
                // Skip collections or properties that require complex mapping logic that ToDto might not handle directly (like Labels)
                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(dtoProp.PropertyType) && dtoProp.PropertyType != typeof(string))
                {
                    continue;
                }

                // Skip properties that don't exist on the entity with the same name
                PropertyInfo? entityProp = entityProps.FirstOrDefault(p => p.Name == dtoProp.Name);
                if (entityProp == null)
                {
                    continue;
                }

                // Skip properties where types are incompatible (though usually mapping handles simple conversions)
                if (!dtoProp.PropertyType.IsAssignableFrom(entityProp.PropertyType))
                {
                    continue;
                }

                object? entityValue = entityProp.GetValue(contact);
                object? dtoValue = dtoProp.GetValue(dto);

                Assert.Equal(entityValue, dtoValue);
            }
        }
    }
    public class AttachmentDtoMappingTests
    {
        [Fact]
        public void ToDtoShouldMapAllPropertiesWhenTheyArePopulated()
        {
            Guid attachmentId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            Attachment attachment = new()
            {
                Id = attachmentId,
                FileName = "test-document.pdf",
                ContentType = "application/pdf",
                AttachmentType = "Document",
                ContactId = contactId
            };

            AttachmentDto dto = attachment.ToDto();

            Assert.Equal(attachmentId, dto.Id);
            Assert.Equal("test-document.pdf", dto.FileName);
            Assert.Equal("application/pdf", dto.ContentType);
            Assert.Equal("Document", dto.AttachmentType);
            Assert.Equal(contactId, dto.EntityId);
            Assert.Equal(EntityType.Person, dto.EntityType);
        }

        [Fact]
        public void ToDtoShouldMapNullFileNameToEmptyString()
        {
            Attachment attachment = new()
            {
                Id = Guid.NewGuid(),
                FileName = null,
                ContentType = "image/png",
                AttachmentType = "Image",
                ContactId = Guid.NewGuid()
            };

            AttachmentDto dto = attachment.ToDto();

            Assert.Equal(string.Empty, dto.FileName);
        }

        [Fact]
        public void ToDtoShouldMapNullContactIdToEmptyGuid()
        {
            Attachment attachment = new()
            {
                Id = Guid.NewGuid(),
                FileName = "orphaned.txt",
                ContentType = "text/plain",
                AttachmentType = "Note",
                ContactId = null
            };

            AttachmentDto dto = attachment.ToDto();

            Assert.Equal(Guid.Empty, dto.EntityId);
        }

        [Fact]
        public void ToDtoShouldAlwaysMapEntityTypeToPerson()
        {
            Attachment attachment = new()
            {
                Id = Guid.NewGuid(),
                FileName = "test.jpg",
                ContentType = "image/jpeg",
                AttachmentType = "ProfileImage",
                ContactId = Guid.NewGuid()
            };

            AttachmentDto dto = attachment.ToDto();

            Assert.Equal(EntityType.Person, dto.EntityType);
        }
    }
    public class ContactDetailDtoMappingTests
    {
        [Fact]
        public void ToDetailDtoShouldMapSimplePropertiesCorrectly()
        {
            Contact contact = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "TestFirst",
                LastName = "TestLast",
                Nickname = "Tester",
                JobTitle = "Developer",
                Company = "TechCorp",
                IsHidden = false,
                Pronouns = "They/Them",
                Gender = "Non-binary",
                Religion = "Agnostic",
                ProfileImageId = Guid.NewGuid(),
                IsPartial = false
            };

            ContactDetailDto result = contact.ToDetailDto();

            Assert.Equal(contact.Id, result.Id);
            Assert.Equal(contact.FirstName, result.FirstName);
            Assert.Equal(contact.LastName, result.LastName);
            Assert.Equal(contact.FullName, result.FullName); // calculated property
            Assert.Equal(contact.Nickname, result.Nickname);
            Assert.Equal(contact.JobTitle, result.JobTitle);
            Assert.Equal(contact.Company, result.Company);
            Assert.Equal(contact.IsHidden, result.IsHidden);
            Assert.Equal(contact.Pronouns, result.Pronouns);
            Assert.Equal(contact.Gender, result.Gender);
            Assert.Equal(contact.Religion, result.Religion);
            Assert.Equal(contact.ProfileImageId, result.ProfileImageId);
            Assert.Equal(contact.IsPartial, result.IsPartial);
        }

        [Fact]
        public void ToDetailDtoShouldMapCollectionsCorrectly()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new()
            {
                Id = contactId,
                FirstName = "Test",
                LastName = "Contact",
                Notes = [new Note { Id = Guid.NewGuid(), Title = "Note1", Value = "Content1", ContactId = contactId }],
                SignificantDates = [new SignificantDate { Id = Guid.NewGuid(), Title = "Birthday", ContactId = contactId }],
                Relationships = [new Relationship { Id = Guid.NewGuid(), EntityId = contactId, RelatedEntityId = Guid.NewGuid() }],
                RelatedTo = [new Relationship { Id = Guid.NewGuid(), EntityId = Guid.NewGuid(), RelatedEntityId = contactId }],
                ContactMethods = [new ContactMethod { Id = Guid.NewGuid(), Label = "Email", Value = "test@example.com", ContactId = contactId }],
                Facts = [new Fact { Id = Guid.NewGuid(), Category = "Fun Fact", Value = "Likes testing", ContactId = contactId }],
                Attachments = [new Attachment { Id = Guid.NewGuid(), FileName = "test.pdf", ContactId = contactId }]
            };

            ContactDetailDto result = contact.ToDetailDto();

            Assert.Single(result.Notes);
            Assert.Equal(contact.Notes.First().Id, result.Notes.First().Id);

            Assert.Single(result.SignificantDates);
            Assert.Equal(contact.SignificantDates.First().Id, result.SignificantDates.First().Id);

            Assert.Single(result.Relationships);
            Assert.Equal(contact.Relationships.First().Id, result.Relationships.First().Id);

            Assert.Single(result.RelatedTo);
            Assert.Equal(contact.RelatedTo.First().Id, result.RelatedTo.First().Id);

            Assert.Single(result.ContactMethods);
            Assert.Equal(contact.ContactMethods.First().Id, result.ContactMethods.First().Id);

            Assert.Single(result.Facts);
            Assert.Equal(contact.Facts.First().Id, result.Facts.First().Id);

            Assert.Single(result.Attachments);
            Assert.Equal(contact.Attachments.First().Id, result.Attachments.First().Id);
        }

        [Fact]
        public void ToDetailDtoShouldHandleNullCollectionsReturnsEmptyLists()
        {
            Contact contact = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "Contact",
                // Explicitly setting collections to null (default behavior, but being explicit for test clarity)
                // Using null! to suppress nullable warnings because we are testing behavior when they are null
                Notes = null!,
                SignificantDates = null!,
                Relationships = null!,
                RelatedTo = null!,
                ContactMethods = null!,
                Facts = null!,
                Attachments = null!
            };

            ContactDetailDto result = contact.ToDetailDto();

            Assert.NotNull(result.Notes);
            Assert.Empty(result.Notes);

            Assert.NotNull(result.SignificantDates);
            Assert.Empty(result.SignificantDates);

            Assert.NotNull(result.Relationships);
            Assert.Empty(result.Relationships);

            Assert.NotNull(result.RelatedTo);
            Assert.Empty(result.RelatedTo);

            Assert.NotNull(result.ContactMethods);
            Assert.Empty(result.ContactMethods);

            Assert.NotNull(result.Facts);
            Assert.Empty(result.Facts);

            Assert.NotNull(result.Attachments);
            Assert.Empty(result.Attachments);

            // Pets are not mapped in ToDetailDto, so we don't assert on them here based on the provided code snippet
        }
    }
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
            Assert.Equal(EntityType.Person, dto.EntityType);
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
            Assert.Equal(EntityType.Person, dto.EntityType);
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
    public class FactDtoMappingTests
    {
        [Fact]
        public void ToDtoShouldMapPropertiesCorrectly()
        {
            Fact fact = new()
            {
                Id = Guid.NewGuid(),
                Category = "Test Category",
                Value = "Test Value",
                ContactId = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow
            };

            FactDto dto = fact.ToDto();

            Assert.Equal(fact.Id, dto.Id);
            Assert.Equal(fact.Category, dto.Category);
            Assert.Equal(fact.Value, dto.Value);
            Assert.Equal(fact.ContactId.Value, dto.EntityId);
            Assert.Equal(EntityType.Person, dto.EntityType);
            Assert.Equal(fact.CreatedDate, dto.CreatedDate);
        }

        [Fact]
        public void ToDtoShouldMapNullContactIdToEmptyGuid()
        {
            Fact fact = new()
            {
                Id = Guid.NewGuid(),
                Category = "Test Category",
                Value = "Test Value",
                ContactId = null,
                CreatedDate = DateTime.UtcNow
            };

            FactDto dto = fact.ToDto();

            Assert.Equal(Guid.Empty, dto.EntityId);
        }

        [Fact]
        public void ToEntityShouldCreateNewFactWithCorrectProperties()
        {
            FactFormDto formDto = new()
            {
                Category = "New Category",
                Value = "New Value",
                EntityId = Guid.NewGuid()
            };

            Fact entity = formDto.ToEntity();

            Assert.NotEqual(Guid.Empty, entity.Id);
            Assert.Equal(formDto.Category, entity.Category);
            Assert.Equal(formDto.Value, entity.Value);
            Assert.Equal(formDto.EntityId, entity.ContactId);
        }

        [Fact]
        public void UpdateEntityShouldUpdatePropertiesCorrectly()
        {
            Guid initialContactId = Guid.NewGuid();
            Fact fact = new()
            {
                Id = Guid.NewGuid(),
                Category = "Old Category",
                Value = "Old Value",
                ContactId = initialContactId
            };

            FactFormDto formDto = new()
            {
                Category = "Updated Category",
                Value = "Updated Value",
                // EntityId in DTO might be different but should be ignored by UpdateEntity
                EntityId = Guid.NewGuid()
            };

            fact.UpdateEntity(formDto);

            Assert.Equal("Updated Category", fact.Category);
            Assert.Equal("Updated Value", fact.Value);

            Assert.Equal(initialContactId, fact.ContactId);
        }
    }
    public class NoteDtoMappingTests
    {
        [Fact]
        public void ToDtoShouldMapPropertiesCorrectly()
        {
            Note note = new()
            {
                Id = Guid.NewGuid(),
                Title = "Test Title",
                Value = "Test Value",
                ContactId = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow
            };

            NoteDto dto = note.ToDto();

            Assert.Equal(note.Id, dto.Id);
            Assert.Equal(note.Title, dto.Title);
            Assert.Equal(note.Value, dto.Value);
            Assert.Equal(note.ContactId.Value, dto.EntityId);
            Assert.Equal(EntityType.Person, dto.EntityType);
            Assert.Equal(note.CreatedDate, dto.CreatedDate);
        }

        [Fact]
        public void ToDtoShouldMapNullContactIdToEmptyGuid()
        {
            Note note = new()
            {
                Id = Guid.NewGuid(),
                Title = "Test Title",
                Value = "Test Value",
                ContactId = null,
                CreatedDate = DateTime.UtcNow
            };

            NoteDto dto = note.ToDto();

            Assert.Equal(Guid.Empty, dto.EntityId);
        }

        [Fact]
        public void ToEntityShouldCreateNewNoteWithCorrectProperties()
        {
            NoteFormDto formDto = new()
            {
                Title = "New Title",
                Value = "New Value",
                EntityId = Guid.NewGuid()
            };

            Note entity = formDto.ToEntity();

            Assert.NotEqual(Guid.Empty, entity.Id);
            Assert.Equal(formDto.Title, entity.Title);
            Assert.Equal(formDto.Value, entity.Value);
            Assert.Equal(formDto.EntityId, entity.ContactId);
        }

        [Fact]
        public void UpdateEntityShouldUpdatePropertiesCorrectly()
        {
            Guid initialContactId = Guid.NewGuid();
            Note note = new()
            {
                Id = Guid.NewGuid(),
                Title = "Old Title",
                Value = "Old Value",
                ContactId = initialContactId
            };

            NoteFormDto formDto = new()
            {
                Title = "Updated Title",
                Value = "Updated Value",
                // EntityId in DTO might be different but should be ignored by UpdateEntity
                EntityId = Guid.NewGuid()
            };

            note.UpdateEntity(formDto);

            Assert.Equal("Updated Title", note.Title);
            Assert.Equal("Updated Value", note.Value);

            Assert.Equal(initialContactId, note.ContactId);
        }
    }
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
    public class SignificantDateDtoMappingTests
    {
        [Fact]
        public void ToDtoShouldMapAllPropertiesWhenEntityIsFullyPopulated()
        {
            SignificantDate entity = new()
            {
                Id = Guid.NewGuid(),
                Title = "Anniversary",
                EventDate = new DateOnly(2023, 10, 27),
                Description = "A special day",
                ContactId = Guid.NewGuid(),
                RecurrenceType = Core.Enumerations.RecurrenceType.Annual,
                CustomIntervalDays = null,
                IsActive = true
            };

            SignificantDateDto dto = entity.ToDto();

            Assert.Equal(entity.Id, dto.Id);
            Assert.Equal(entity.Title, dto.Title);
            Assert.Equal(entity.EventDate, dto.EventDate);
            Assert.Equal(entity.Description, dto.Description);
            Assert.Equal(entity.ContactId, dto.EntityId);
            Assert.Equal(EntityType.Person, dto.EntityType);
            Assert.Equal(entity.RecurrenceType, dto.RecurrenceType);
            Assert.Equal(entity.CustomIntervalDays, dto.CustomIntervalDays);
            Assert.Equal(entity.IsActive, dto.IsActive);
        }

        [Fact]
        public void ToDtoShouldMapNullTitleToEmptyString()
        {
            SignificantDate entity = new()
            {
                Title = null,
                EventDate = DateOnly.FromDateTime(DateTime.Now)
            };

            SignificantDateDto dto = entity.ToDto();

            Assert.Equal(string.Empty, dto.Title);
        }

        [Fact]
        public void ToDtoShouldMapNullContactIdToEmptyGuid()
        {
            SignificantDate entity = new()
            {
                ContactId = null,
                EventDate = DateOnly.FromDateTime(DateTime.Now)
            };

            SignificantDateDto dto = entity.ToDto();

            Assert.Equal(Guid.Empty, dto.EntityId);
        }

        [Fact]
        public void ToDtoShouldSetEntityTypeToPerson()
        {
            SignificantDate entity = new()
            {
                EventDate = DateOnly.FromDateTime(DateTime.Now)
            };

            SignificantDateDto dto = entity.ToDto();

            Assert.Equal(EntityType.Person, dto.EntityType);
        }

        [Fact]
        public void ToDtoShouldMapNullDescriptionAsNull()
        {
            SignificantDate entity = new()
            {
                Description = null,
                EventDate = DateOnly.FromDateTime(DateTime.Now)
            };

            SignificantDateDto dto = entity.ToDto();

            Assert.Null(dto.Description);
        }
    }
}
