using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.DTOs.Pet;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Xunit;

namespace Rvnx.CRM.Tests
{
    public class DtoPropertyTests
    {
        private void VerifyProperties<TModel, TDto>(string[]? ignoreProperties = null)
        {
            var modelProperties = typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var dtoProperties = typeof(TDto).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var dtoPropertyNames = dtoProperties.Select(p => p.Name).ToHashSet();

            var missingProperties = new List<string>();

            // Convert ignoreProperties to HashSet for faster lookup if not null
            var ignoredSet = ignoreProperties != null
                ? new HashSet<string>(ignoreProperties)
                : new HashSet<string>();

            foreach (var property in modelProperties)
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
            // Contact inherits Person, CRMBaseEntity
            VerifyProperties<Contact, ContactDetailDto>();
        }

        [Fact]
        public void PetDto_ShouldHave_All_Pet_Properties()
        {
            // Pet inherits CRMGenericEntity, CRMBaseEntity
            VerifyProperties<Pet, PetDto>();
        }

        [Fact]
        public void NoteDto_ShouldHave_All_Note_Properties()
        {
            // Note inherits CRMGenericEntity, CRMBaseEntity
            VerifyProperties<Note, NoteDto>();
        }

        [Fact]
        public void ImportantDateDto_ShouldHave_All_ImportantDate_Properties()
        {
            // ImportantDate inherits CRMGenericEntity, CRMBaseEntity
            VerifyProperties<ImportantDate, ImportantDateDto>();
        }

        [Fact]
        public void ReminderDto_ShouldHave_All_Reminder_Properties()
        {
            // Reminder inherits CRMGenericEntity, CRMBaseEntity
            VerifyProperties<Reminder, ReminderDto>();
        }

        [Fact]
        public void RelationshipDto_ShouldHave_All_Relationship_Properties()
        {
            // Relationship inherits CRMGenericEntity, CRMBaseEntity
            VerifyProperties<Relationship, RelationshipDto>(new[] { "RelationshipType", "Person", "RelatedPerson" });
        }
    }
}
