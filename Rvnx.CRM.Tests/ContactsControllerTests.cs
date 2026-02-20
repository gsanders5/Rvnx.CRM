using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests
{
    public class ContactsControllerTests : IDisposable
    {
        private readonly CRMDbContext _context;
        private readonly Mock<ILogger<ContactsController>> _loggerMock = new();
        private readonly Mock<ICurrentUserService> _userMock = new();
        private readonly Mock<IVCardService> _vCardMock = new();
        private readonly Mock<IFileValidationService> _fileValidationMock = new();
        private readonly Mock<IUserSynchronizationService> _syncMock = new();
        private readonly Mock<IContactImportService> _contactImportMock = new();
        private readonly ContactsController _controller;

        public ContactsControllerTests()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _userMock.Setup(s => s.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"));
            _userMock.Setup(s => s.UserName).Returns("test-user");

            _syncMock.Setup(s => s.SyncUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns(Task.CompletedTask);

            _context = new CRMDbContext(options, _userMock.Object);
            Repository repository = new Repository(_context);
            
            _controller = new ContactsController(
                repository, 
                _loggerMock.Object, 
                _userMock.Object, 
                _vCardMock.Object, 
                _fileValidationMock.Object, 
                _syncMock.Object,
                _contactImportMock.Object);
                
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            _controller.TempData = new TempDataDictionary(_controller.HttpContext, Mock.Of<ITempDataProvider>());
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task Index_ReturnsViewWithContacts()
        {
            // Arrange
            _context.Contacts.Add(new Contact { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" });
            _context.Contacts.Add(new Contact { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe" });
            await _context.SaveChangesAsync();

            // Act
            IActionResult result = await _controller.Index();

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            List<ContactDto> model = Assert.IsAssignableFrom<List<ContactDto>>(viewResult.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task Create_Post_WithValidData_ShouldCreateContactAndRelatedEntities()
        {
            // Arrange
            ContactFormDto dto = new()
            {
                FirstName = "New",
                LastName = "User",
                Email = "new@user.com",
                Phone = "1234567890",
                Birthday = new DateTime(1990, 1, 1)
            };

            // Act
            IActionResult result = await _controller.Create(dto);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            Contact? contact = await _context.Contacts.FirstOrDefaultAsync(c => c.FirstName == "New");
            Assert.NotNull(contact);

            // Check Email
            ContactMethod? email = await _context.Set<ContactMethod>().FirstOrDefaultAsync(c => c.EntityId == contact.Id && c.Type == ContactMethodType.Email);
            Assert.NotNull(email);
            Assert.Equal("new@user.com", email.Value);

            // Check Phone
            ContactMethod? phone = await _context.Set<ContactMethod>().FirstOrDefaultAsync(c => c.EntityId == contact.Id && c.Type == ContactMethodType.Phone);
            Assert.NotNull(phone);
            Assert.Equal("1234567890", phone.Value);

            // Check Birthday
            SignificantDate? birthday = await _context.Set<SignificantDate>().FirstOrDefaultAsync(d => d.EntityId == contact.Id && d.Title == SignificantDateTitles.Birthday);
            Assert.NotNull(birthday);
            Assert.Equal(new DateTime(1990, 1, 1), birthday.Date);
        }

        [Fact]
        public async Task DeleteConfirmed_WithValidId_ShouldDeleteContactAndDependencies()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "To", LastName = "Delete" };
            _context.Contacts.Add(contact);
            _context.Set<Note>().Add(new Note { Id = Guid.NewGuid(), EntityId = contactId, EntityType = EntityTypes.Person, Title = "Note", Value = "Val" });
            await _context.SaveChangesAsync();

            // Act
            IActionResult result = await _controller.DeleteConfirmed(contactId);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            Assert.Null(await _context.Contacts.FindAsync(contactId));
            Assert.Empty(await _context.Set<Note>().Where(n => n.EntityId == contactId).ToListAsync());
        }

        [Fact]
        public async Task Edit_Post_WhenFileIsNotImage_ShouldReturnValidationError()
        {
            // Arrange
            _fileValidationMock.Setup(f => f.IsImageExtension(It.IsAny<string>())).Returns(false);

            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "John", LastName = "Doe" };
            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();

            ContactFormDto dto = new() { Id = contactId, FirstName = "John", LastName = "Doe" };

            Mock<IFormFile> fileMock = new();
            string content = "This is not an image";
            MemoryStream ms = new();
            StreamWriter writer = new(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.FileName).Returns("test.txt");
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.ContentType).Returns("text/plain");

            // Act
            IActionResult result = await _controller.Edit(contactId, dto, fileMock.Object);

            // Assert
            Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.True(_controller.ModelState.ContainsKey(string.Empty));
            Assert.Equal("Only image files (jpg, jpeg, png, gif) are allowed.", _controller.ModelState[string.Empty]!.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Edit_Post_WhenFileSignatureInvalid_ShouldReturnError()
        {
            // Arrange
            _fileValidationMock.Setup(f => f.IsImageExtension(It.IsAny<string>())).Returns(true);
            _fileValidationMock.Setup(f => f.IsValidImageSignature(It.IsAny<byte[]>(), It.IsAny<string>())).Returns(false);

            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "John", LastName = "Doe" };
            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();

            ContactFormDto dto = new() { Id = contactId, FirstName = "John", LastName = "Doe" };

            Mock<IFormFile> fileMock = new();
            string content = "Fake Image Content";
            MemoryStream ms = new();
            StreamWriter writer = new(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((stream, _) =>
                {
                    ms.CopyTo(stream);
                })
                .Returns(Task.CompletedTask);
            fileMock.Setup(f => f.FileName).Returns("fake.png");
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.ContentType).Returns("image/png");

            // Act
            IActionResult result = await _controller.Edit(contactId, dto, fileMock.Object);

            // Assert
            Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.True(_controller.ModelState.ContainsKey(string.Empty));
            Assert.Equal("Invalid file signature.", _controller.ModelState[string.Empty]!.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Edit_Post_WhenFileIsImage_ShouldSaveAttachment()
        {
            // Arrange
            _fileValidationMock.Setup(f => f.IsImageExtension(It.IsAny<string>())).Returns(true);
            _fileValidationMock.Setup(f => f.IsValidImageSignature(It.IsAny<byte[]>(), It.IsAny<string>())).Returns(true);

            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "John", LastName = "Doe" };
            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();

            ContactFormDto dto = new() { Id = contactId, FirstName = "John", LastName = "Doe" };

            Mock<IFormFile> fileMock = new();
            string content = "Fake Image Content";
            MemoryStream ms = new();
            StreamWriter writer = new(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((stream, _) =>
                {
                    ms.CopyTo(stream);
                })
                .Returns(Task.CompletedTask);
            fileMock.Setup(f => f.FileName).Returns("image.png");
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.ContentType).Returns("image/png");

            // Act
            IActionResult result = await _controller.Edit(contactId, dto, fileMock.Object);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            // Use Set<Attachment> since Attachments might not exist on context directly
            List<Attachment> attachments = await _context.Set<Attachment>().Where(a => a.EntityId == contactId && a.AttachmentType == AttachmentTypes.ProfileImage).ToListAsync();
            Assert.Single(attachments);
            Assert.Equal("image.png", attachments[0].FileName);
        }

        [Fact]
        public async Task Import_ShouldCallServiceAndRedirect()
        {
            // Arrange
            Mock<IFormFile> fileMock = new();
            fileMock.Setup(f => f.Length).Returns(100);
            fileMock.Setup(f => f.FileName).Returns("contacts.vcf");
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

            _contactImportMock.Setup(s => s.ImportContactsAsync(It.IsAny<Stream>())).ReturnsAsync((1, 1));

            // Act
            IActionResult result = await _controller.Import(fileMock.Object);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            // Verify message
            Assert.Equal("Import successful! Added: 1, Skipped: 1", _controller.TempData["SuccessMessage"]);
            _contactImportMock.Verify(s => s.ImportContactsAsync(It.IsAny<Stream>()), Times.Once);
        }
    }
}
