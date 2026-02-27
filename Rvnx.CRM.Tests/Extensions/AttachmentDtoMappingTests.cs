using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Models.Base;

namespace Rvnx.CRM.Tests.Extensions
{
    public class AttachmentDtoMappingTests
    {
        [Fact]
        public void ToDtoShouldMapAllPropertiesWhenTheyArePopulated()
        {
            // Arrange
            Guid attachmentId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            Attachment attachment = new()
            {
                Id = attachmentId,
                FileName = "test-document.pdf",
                ContentType = "application/pdf",
                AttachmentType = "Document",
                ContactId = contactId
            };

            // Act
            AttachmentDto dto = attachment.ToDto();

            // Assert
            Assert.Equal(attachmentId, dto.Id);
            Assert.Equal("test-document.pdf", dto.FileName);
            Assert.Equal("application/pdf", dto.ContentType);
            Assert.Equal("Document", dto.AttachmentType);
            Assert.Equal(contactId, dto.EntityId);
            Assert.Equal(EntityTypes.Person, dto.EntityType);
        }

        [Fact]
        public void ToDtoShouldMapNullFileNameToEmptyString()
        {
            // Arrange
            Attachment attachment = new()
            {
                Id = Guid.NewGuid(),
                FileName = null,
                ContentType = "image/png",
                AttachmentType = "Image",
                ContactId = Guid.NewGuid()
            };

            // Act
            AttachmentDto dto = attachment.ToDto();

            // Assert
            Assert.Equal(string.Empty, dto.FileName);
        }

        [Fact]
        public void ToDtoShouldMapNullContactIdToEmptyGuid()
        {
            // Arrange
            Attachment attachment = new()
            {
                Id = Guid.NewGuid(),
                FileName = "orphaned.txt",
                ContentType = "text/plain",
                AttachmentType = "Note",
                ContactId = null
            };

            // Act
            AttachmentDto dto = attachment.ToDto();

            // Assert
            Assert.Equal(Guid.Empty, dto.EntityId);
        }

        [Fact]
        public void ToDtoShouldAlwaysMapEntityTypeToPerson()
        {
            // Arrange
            Attachment attachment = new()
            {
                Id = Guid.NewGuid(),
                FileName = "test.jpg",
                ContentType = "image/jpeg",
                AttachmentType = "ProfileImage",
                ContactId = Guid.NewGuid()
            };

            // Act
            AttachmentDto dto = attachment.ToDto();

            // Assert
            Assert.Equal(EntityTypes.Person, dto.EntityType);
        }
    }
}
