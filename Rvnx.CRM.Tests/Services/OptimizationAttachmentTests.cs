using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services
{
    public class OptimizationAttachmentTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<IFileValidationService> _fileValidationServiceMock;
        private readonly ContactManagementService _service;

        public OptimizationAttachmentTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _fileValidationServiceMock = new Mock<IFileValidationService>();
            _service = new ContactManagementService(_repositoryMock.Object, _fileValidationServiceMock.Object);
        }

        [Fact]
        public async Task ArchiveExistingProfilePhotoAsyncShouldUseUpdateRange()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            List<Attachment> attachments =
            [
                new Attachment { Id = Guid.NewGuid(), ContactId = contactId, AttachmentType = "ProfileImage" },
                new Attachment { Id = Guid.NewGuid(), ContactId = contactId, AttachmentType = "ProfileImage" },
                new Attachment { Id = Guid.NewGuid(), ContactId = contactId, AttachmentType = "ProfileImage" }
            ];

            _repositoryMock.Setup(r => r.ListAsync(It.IsAny<Expression<Func<Attachment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(attachments);

            // Act
            await _service.UnsetProfilePhotoAsync(contactId);

            // Assert
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Attachment>(), It.IsAny<CancellationToken>()), Times.Never());
            _repositoryMock.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}
