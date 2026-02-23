using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using System.Reflection;

namespace Rvnx.CRM.Tests.DTOs
{
    public class DtoPropertyTests
    {
        private static void VerifyProperties<TModel, TDto>(string[]? ignoreProperties = null)
        {
            PropertyInfo[] modelProperties = typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo[] dtoProperties = typeof(TDto).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            HashSet<string> dtoPropertyNames = dtoProperties.Select(p => p.Name).ToHashSet();

            List<string> missingProperties = [];

            // Convert ignoreProperties to HashSet for faster lookup if not null
            HashSet<string> ignoredSet = ignoreProperties != null
                ? [.. ignoreProperties]
                : [];

            foreach (PropertyInfo property in modelProperties)
            {
                if (ignoredSet.Contains(property.Name))
                {
                    continue;
                }

                if (!dtoPropertyNames.Contains(property.Name))
                {
                    missingProperties.Add(property.Name);
                }
            }

            Assert.True(missingProperties.Count == 0,
                $"Missing properties in {typeof(TDto).Name} from {typeof(TModel).Name}: {string.Join(", ", missingProperties)}");
        }

        private static readonly string[] ContactDetailDtoIgnoredProperties = ["IsHidden", "Addresses", "Attachments"];
        [Fact]
        public void ContactDetailDtoShouldHaveAllContactProperties()
        {
            // Contact inherits Person, BaseEntity
            VerifyProperties<Contact, ContactDetailDto>(ContactDetailDtoIgnoredProperties); // Handled as collections in DetailDto
        }

        private static readonly string[] PetDtoIgnoredProperties = ["ContactId", "Contact"];
        [Fact]
        public void PetDtoShouldHaveAllPetProperties()
        {
            // Pet inherits BaseEntity
            // ContactId/Contact are navigation/FKs, often not in DTO or named EntityId
            VerifyProperties<Pet, PetDto>(PetDtoIgnoredProperties);
        }

        private static readonly string[] NoteDtoIgnoredProperties = ["ContactId", "Contact"];
        [Fact]
        public void NoteDtoShouldHaveAllNoteProperties()
        {
            // Note inherits BaseEntity
            VerifyProperties<Note, NoteDto>(NoteDtoIgnoredProperties);
        }

        private static readonly string[] SignificantDateDtoIgnoredProperties = ["ContactId", "Contact"];
        [Fact]
        public void SignificantDateDtoShouldHaveAllSignificantDateProperties()
        {
            // SignificantDate inherits BaseEntity
            VerifyProperties<SignificantDate, SignificantDateDto>(SignificantDateDtoIgnoredProperties);
        }

        private static readonly string[] ReminderDtoIgnoredProperties = ["ContactId", "Contact"];
        [Fact]
        public void ReminderDtoShouldHaveAllReminderProperties()
        {
            // Reminder inherits BaseEntity
            VerifyProperties<Reminder, ReminderDto>(ReminderDtoIgnoredProperties);
        }

        private static readonly string[] RelationshipDtoIgnoredProperties = ["RelationshipType", "Person", "RelatedPerson"];
        [Fact]
        public void RelationshipDtoShouldHaveAllRelationshipProperties()
        {
            // Relationship inherits PolymorphicEntity, BaseEntity
            VerifyProperties<Relationship, RelationshipDto>(RelationshipDtoIgnoredProperties);
        }
    }
}
