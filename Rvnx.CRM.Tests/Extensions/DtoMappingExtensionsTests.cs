using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Models.Contact;
using Xunit;

namespace Rvnx.CRM.Tests.Extensions
{
    public class DtoMappingExtensionsTests
    {
        [Fact]
        public void ToDtoShouldMapLastChangedDateFromContact()
        {
            // Arrange
            var lastChangedDate = new DateTime(2023, 10, 27, 12, 0, 0, DateTimeKind.Utc);
            var contact = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "Contact",
                LastChangedDate = lastChangedDate
            };

            // Act
            var dto = contact.ToDto();

            // Assert
            Assert.Equal(lastChangedDate, dto.LastChangedDate);
            Assert.NotEqual(DateTime.MinValue, dto.LastChangedDate);
        }
    }
}
