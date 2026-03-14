using Moq;
using Moq.Protected;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Services;
using System.Net;
using System.Text;

namespace Rvnx.CRM.Tests.Services
{
    public class VCardServiceSSRFTests : IDisposable
    {
        private readonly VCardService _service;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;

        public VCardServiceSSRFTests()
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
        public async Task ParseVCardAsyncShouldBlockPrivateIp()
        {
            string privateIpUrl = "http://192.168.1.1/photo.jpg";
            string vcfContent = CreateVCardWithPhoto(privateIpUrl);

            SetupMockResponse(privateIpUrl, "Private Data");

            using MemoryStream stream = new(Encoding.UTF8.GetBytes(vcfContent));

            IEnumerable<Contact> result = await _service.ParseVCardAsync(stream);
            List<Contact> contacts = result.ToList();

            Assert.Single(contacts);
            Assert.Empty(contacts.First().Attachments);
        }

        [Fact]
        public async Task ParseVCardAsyncShouldBlockLoopback()
        {
            string loopbackUrl = "http://127.0.0.1/photo.jpg";
            string vcfContent = CreateVCardWithPhoto(loopbackUrl);

            SetupMockResponse(loopbackUrl, "Loopback Data");

            using MemoryStream stream = new(Encoding.UTF8.GetBytes(vcfContent));

            IEnumerable<Contact> result = await _service.ParseVCardAsync(stream);
            List<Contact> contacts = result.ToList();

            Assert.Single(contacts);
            Assert.Empty(contacts.First().Attachments);
        }

        [Fact]
        public async Task ParseVCardAsyncShouldBlockAnyAddress()
        {
            string anyUrl = "http://0.0.0.0/photo.jpg";
            string vcfContent = CreateVCardWithPhoto(anyUrl);

            SetupMockResponse(anyUrl, "Any Data");

            using MemoryStream stream = new(Encoding.UTF8.GetBytes(vcfContent));

            IEnumerable<Contact> result = await _service.ParseVCardAsync(stream);
            List<Contact> contacts = result.ToList();

            Assert.Single(contacts);
            Assert.Empty(contacts.First().Attachments);
        }

        [Fact]
        public async Task ParseVCardAsyncShouldBlockIPv6MappedLoopback()
        {
            string mappedUrl = "http://[::ffff:127.0.0.1]/photo.jpg";
            string vcfContent = CreateVCardWithPhoto(mappedUrl);

            SetupMockResponse(mappedUrl, "Mapped Loopback Data");

            using MemoryStream stream = new(Encoding.UTF8.GetBytes(vcfContent));

            IEnumerable<Contact> result = await _service.ParseVCardAsync(stream);
            List<Contact> contacts = result.ToList();

            Assert.Single(contacts);
            Assert.Empty(contacts.First().Attachments);
        }

        [Fact]
        public async Task ParseVCardAsyncShouldAllowPublicIp()
        {
            string publicIpUrl = "http://93.184.216.34/photo.jpg";
            string vcfContent = CreateVCardWithPhoto(publicIpUrl);

            SetupMockResponse(publicIpUrl, "Public Data");

            using MemoryStream stream = new(Encoding.UTF8.GetBytes(vcfContent));

            IEnumerable<Contact> result = await _service.ParseVCardAsync(stream);
            List<Contact> contacts = result.ToList();

            Assert.Single(contacts);
            Contact contact = contacts.First();

            // Should have attachment because public IP is allowed
            Assert.Single(contact.Attachments);
            Attachment attachment = contact.Attachments.First();
            Assert.Equal("Public Data", Encoding.UTF8.GetString(attachment.AttachmentContent!.Content));
        }

        private static string CreateVCardWithPhoto(string url)
        {
            return $@"BEGIN:VCARD
VERSION:3.0
FN:Test
N:Test;;;;
PHOTO;VALUE=URI:{url}
END:VCARD";
        }

        private void SetupMockResponse(string url, string content)
        {
            _httpMessageHandlerMock.Protected()
               .Setup<Task<HttpResponseMessage>>(
                   "SendAsync",
                   ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri == new Uri(url)),
                   ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new ByteArrayContent(Encoding.UTF8.GetBytes(content))
               });
        }
    }
}