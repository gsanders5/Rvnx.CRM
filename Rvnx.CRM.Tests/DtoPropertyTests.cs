using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.DTOs.Pet;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using System.Reflection;

namespace Rvnx.CRM.Tests
{
    public class DtoPropertyTests
    {
        private void VerifyProperties<TModel, TDto>(string[]? ignoreProperties = null)
        {
            PropertyInfo[] modelProperties = typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo[] dtoProperties = typeof(TDto).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            HashSet<string> dtoPropertyNames = dtoProperties.Select(p => p.Name).ToHashSet();

            List<string> missingProperties = new();

            // Convert ignoreProperties to HashSet for faster lookup if not null
            HashSet<string> ignoredSet = ignoreProperties != null
                ? new HashSet<string>(ignoreProperties)
                : new HashSet<string>();

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

        [Fact]
        public void ContactDetailDto_ShouldHave_All_Contact_Properties()
        {
            // Contact inherits Person, BaseEntity
            VerifyProperties<Contact, ContactDetailDto>(new[] { "IsHidden", "Addresses", "Attachments", "LinkedUser" }); // Handled as collections in DetailDto
        }

        [Fact]
        public void PetDto_ShouldHave_All_Pet_Properties()
        {
            // Pet inherits PolymorphicEntity, BaseEntity
            VerifyProperties<Pet, PetDto>();
        }

        [Fact]
        public void NoteDto_ShouldHave_All_Note_Properties()
        {
            // Note inherits PolymorphicEntity, BaseEntity
            VerifyProperties<Note, NoteDto>();
        }

        [Fact]
        public void SignificantDateDto_ShouldHave_All_SignificantDate_Properties()
        {
            // SignificantDate inherits PolymorphicEntity, BaseEntity
            VerifyProperties<SignificantDate, SignificantDateDto>();
        }

        [Fact]
        public void ReminderDto_ShouldHave_All_Reminder_Properties()
        {
            // Reminder inherits PolymorphicEntity, BaseEntity
            VerifyProperties<Reminder, ReminderDto>();
        }

        [Fact]
        public void RelationshipDto_ShouldHave_All_Relationship_Properties()
        {
            // Relationship inherits PolymorphicEntity, BaseEntity
            VerifyProperties<Relationship, RelationshipDto>(new[] { "RelationshipType", "Person", "RelatedPerson" });
        }
    }
}
