using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Models.Contact;
using System.Reflection;

namespace Rvnx.CRM.Tests.Extensions
{
    public class DtoMappingExtensionsTests
    {
        [Fact]
        public void ToDtoShouldMapLastChangedDateFromContact()
        {
            // Arrange
            DateTime lastChangedDate = new(2023, 10, 27, 12, 0, 0, DateTimeKind.Utc);
            Contact contact = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "Contact",
                LastChangedDate = lastChangedDate
            };

            // Act
            ContactDto dto = contact.ToDto();

            // Assert
            Assert.Equal(lastChangedDate, dto.LastChangedDate);
            Assert.NotEqual(DateTime.MinValue, dto.LastChangedDate);
        }

        [Fact]
        public void ToDtoShouldMapAllMatchingProperties()
        {
            // Arrange
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

            // Act
            ContactDto dto = contact.ToDto();

            // Assert
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
}
