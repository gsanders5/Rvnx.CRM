using System.Text;
using FolkerKinzel.VCards;
using FolkerKinzel.VCards.Models;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Services;
using Xunit;

namespace Rvnx.CRM.Tests
{
    public class VCardServiceTests
    {
        private readonly VCardService _service;

        public VCardServiceTests()
        {
            _service = new VCardService();
        }

        [Fact]
        public void ParseVCard_ShouldReturnContact_WhenValidVCardProvided()
        {
            // Arrange
            string vcfContent = @"BEGIN:VCARD
VERSION:3.0
N:Doe;John;;;
FN:John Doe
EMAIL;TYPE=INTERNET:john.doe@example.com
TEL;TYPE=CELL:1234567890
BDAY:1990-01-01
ORG:Acme Corp
TITLE:Manager
END:VCARD";

            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(vcfContent));

            // Act
            var contacts = _service.ParseVCard(stream).ToList();

            // Assert
            Assert.Single(contacts);
            var contact = contacts.First();
            Assert.Equal("John", contact.FirstName);
            Assert.Equal("Doe", contact.LastName);
            Assert.Equal("Acme Corp", contact.Company);
            Assert.Equal("Manager", contact.JobTitle);

            Assert.Contains(contact.ContactMethods, cm => cm.Type == ContactMethodType.Email && cm.Value == "john.doe@example.com");
            Assert.Contains(contact.ContactMethods, cm => cm.Type == ContactMethodType.Phone && cm.Value == "1234567890");

            var bday = contact.SignificantDates.FirstOrDefault(sd => sd.Title == "Birthday");
            Assert.NotNull(bday);
            Assert.Equal(new DateTime(1990, 1, 1), bday.Date);
        }

        [Fact]
        public void ExportVCard_ShouldReturnValidVcf_WhenContactProvided()
        {
            // Arrange
            var contact = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Jane",
                LastName = "Smith",
                Company = "Tech Inc",
                JobTitle = "Developer"
            };

            contact.ContactMethods.Add(new ContactMethod
            {
                Type = ContactMethodType.Email,
                Value = "jane@example.com"
            });

            contact.SignificantDates.Add(new SignificantDate
            {
                Title = "Birthday",
                Date = new DateTime(1995, 5, 20)
            });

            // Act
            byte[] bytes = _service.ExportVCard(contact);
            string vcf = Encoding.UTF8.GetString(bytes);

            // Assert
            Assert.Contains("BEGIN:VCARD", vcf);
            Assert.Contains("VERSION:3.0", vcf);
            Assert.Contains("Jane", vcf);
            Assert.Contains("Smith", vcf);
            Assert.Contains("Tech Inc", vcf);
            Assert.Contains("Developer", vcf);
            Assert.Contains("jane@example.com", vcf);
            Assert.Contains("19950520", vcf.Replace("-", "")); // Date format might vary
            Assert.Contains("END:VCARD", vcf);
        }
    }
}
