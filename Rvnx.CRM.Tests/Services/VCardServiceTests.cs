using Moq;
using Moq.Protected;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Services;
using System.Net;
using System.Text;

namespace Rvnx.CRM.Tests.Services
{
    public class VCardServiceTests : IDisposable
    {
        private readonly VCardService _service;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;

        public VCardServiceTests()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _service = new VCardService(_httpClient);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task ParseVCardAsyncShouldReturnContactWhenValidVCardProvided()
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

            using MemoryStream stream = new(Encoding.UTF8.GetBytes(vcfContent));

            // Act
            IEnumerable<Contact> result = await _service.ParseVCardAsync(stream);
            List<Contact> contacts = result.ToList();

            // Assert
            Assert.Single(contacts);
            Contact contact = contacts.First();
            Assert.Equal("John", contact.FirstName);
            Assert.Equal("Doe", contact.LastName);
            Assert.Equal("Acme Corp", contact.Company);
            Assert.Equal("Manager", contact.JobTitle);

            Assert.Contains(contact.ContactMethods, cm => cm.Type == ContactMethodType.Email && cm.Value == "john.doe@example.com");
            Assert.Contains(contact.ContactMethods, cm => cm.Type == ContactMethodType.Phone && cm.Value == "1234567890");

            SignificantDate? bday = contact.SignificantDates.FirstOrDefault(sd => sd.Title == SignificantDateTitles.Birthday);
            Assert.NotNull(bday);
            Assert.Equal(new DateTime(1990, 1, 1), bday.Date);
        }

        [Fact]
        public async Task ParseVCardAsyncShouldExtractEmbeddedPhoto()
        {
            // Arrange
            string vcfContent = @"BEGIN:VCARD
VERSION:3.0
FN:Photo Test
N:Test;Photo;;;
PHOTO;ENCODING=b;TYPE=JPEG:SGVsbG8gV29ybGQ=
END:VCARD";
            // "SGVsbG8gV29ybGQ=" is "Hello World" in Base64

            using MemoryStream stream = new(Encoding.UTF8.GetBytes(vcfContent));

            // Act
            IEnumerable<Contact> result = await _service.ParseVCardAsync(stream);
            List<Contact> contacts = result.ToList();

            // Assert
            Assert.Single(contacts);
            Contact contact = contacts.First();
            Assert.Single(contact.Attachments);
            Attachment attachment = contact.Attachments.First();
            Assert.Equal(AttachmentTypes.ProfileImage, attachment.AttachmentType);
            Assert.Equal("image/jpeg", attachment.ContentType);
            Assert.NotNull(attachment.AttachmentContent);
            Assert.Equal("Hello World", Encoding.UTF8.GetString(attachment.AttachmentContent.Content));
        }

        [Fact]
        public async Task ParseVCardAsyncShouldDownloadUrlPhoto()
        {
            // Arrange
            string photoUrl = "https://example.com/photo.jpg";
            string vcfContent = $@"BEGIN:VCARD
VERSION:3.0
FN:Url Photo Test
N:Test;Url;;;
PHOTO;VALUE=URI:{photoUrl}
END:VCARD";

            byte[] photoBytes = Encoding.UTF8.GetBytes("Downloaded Photo");

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri == new Uri(photoUrl)),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new ByteArrayContent(photoBytes)
                });

            using MemoryStream stream = new(Encoding.UTF8.GetBytes(vcfContent));

            // Act
            IEnumerable<Contact> result = await _service.ParseVCardAsync(stream);
            List<Contact> contacts = result.ToList();

            // Assert
            Assert.Single(contacts);
            Contact contact = contacts.First();
            Assert.Single(contact.Attachments);
            Attachment attachment = contact.Attachments.First();
            Assert.Equal(AttachmentTypes.ProfileImage, attachment.AttachmentType);
            Assert.NotNull(attachment.AttachmentContent);
            Assert.Equal("Downloaded Photo", Encoding.UTF8.GetString(attachment.AttachmentContent.Content));
        }

        [Fact]
        public async Task ParseVCardAsyncShouldHandleGifPhoto()
        {
            // Arrange
            string vcfContent = @"BEGIN:VCARD
VERSION:3.0
FN:Gif Photo Test
N:Test;Gif;;;
PHOTO;ENCODING=b;TYPE=GIF:R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7
END:VCARD";
            // 1x1 transparent GIF Base64

            using MemoryStream stream = new(Encoding.UTF8.GetBytes(vcfContent));

            // Act
            IEnumerable<Contact> result = await _service.ParseVCardAsync(stream);
            List<Contact> contacts = result.ToList();

            // Assert
            Assert.Single(contacts);
            Contact contact = contacts.First();
            Assert.Single(contact.Attachments);
            Attachment attachment = contact.Attachments.First();
            Assert.Equal(AttachmentTypes.ProfileImage, attachment.AttachmentType);
            Assert.Equal("image/gif", attachment.ContentType);
            Assert.Equal("vcard_photo.gif", attachment.FileName);
        }

        [Fact]
        public void ExportVCardShouldReturnValidVcfWhenContactProvided()
        {
            // Arrange
            Contact contact = new()
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
                Title = SignificantDateTitles.Birthday,
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

        [Fact]
        public void ExportVCardShouldEmbedProfileImage()
        {
            // Arrange
            Contact contact = new()
            {
                FirstName = "Photo",
                LastName = "Export"
            };

            byte[] photoBytes = Encoding.UTF8.GetBytes("Exported Photo");
            contact.Attachments.Add(new Attachment
            {
                AttachmentType = AttachmentTypes.ProfileImage,
                ContentType = "image/png",
                AttachmentContent = new AttachmentContent
                {
                    Content = photoBytes
                }
            });

            // Act
            byte[] bytes = _service.ExportVCard(contact);
            string vcf = Encoding.UTF8.GetString(bytes);

            // Assert
            Assert.Contains("PHOTO", vcf);
            // Base64 of "Exported Photo" is "RXhwb3J0ZWQgUGhvdG8="
            Assert.Contains("RXhwb3J0ZWQgUGhvdG8=", vcf);
        }
    }
}
