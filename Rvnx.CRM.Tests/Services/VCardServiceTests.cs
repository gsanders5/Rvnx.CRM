using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Services;
using System.Text;

namespace Rvnx.CRM.Tests.Services
{
    public class VCardServiceTests
    {
        private readonly VCardService _service;

        public VCardServiceTests()
        {
            _service = new VCardService();
        }

        [Fact]
        public async Task ParseVCardAsyncWhenInvalidStreamReturnsEmpty()
        {
            // Arrange
            string invalidContent = "This is not a valid VCard content";
            using MemoryStream stream = new(Encoding.UTF8.GetBytes(invalidContent));

            // Act
            IEnumerable<Contact> result = await _service.ParseVCardAsync(stream);

            // Assert
            Assert.Empty(result);
        }
    }
}