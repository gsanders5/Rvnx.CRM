using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Models.Base;
using Xunit;

namespace Rvnx.CRM.Tests.Extensions
{
    public class AttachmentDtoMappingTests
    {
        [Fact]
        public void ToDtoShouldMapAllPropertiesWhenTheyArePopulated()
        {
            // Arrange
            var attachmentId = Guid.NewGuid();
            var contactId = Guid.NewGuid();
            var attachment = new Attachment
            {
                Id = attachmentId,
                FileName = "test-document.pdf",
                ContentType = "application/pdf",
                AttachmentType = "Document",
                ContactId = contactId
            };

            // Act
            var dto = attachment.ToDto();

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
            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),
                FileName = null,
                ContentType = "image/png",
                AttachmentType = "Image",
                ContactId = Guid.NewGuid()
            };

            // Act
            var dto = attachment.ToDto();

            // Assert
            Assert.Equal(string.Empty, dto.FileName);
        }

        [Fact]
        public void ToDtoShouldMapNullContactIdToEmptyGuid()
        {
            // Arrange
            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),
                FileName = "orphaned.txt",
                ContentType = "text/plain",
                AttachmentType = "Note",
                ContactId = null
            };

            // Act
            var dto = attachment.ToDto();

            // Assert
            Assert.Equal(Guid.Empty, dto.EntityId);
        }

        [Fact]
        public void ToDtoShouldAlwaysMapEntityTypeToPerson()
        {
            // Arrange
            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),
                FileName = "test.jpg",
                ContentType = "image/jpeg",
                AttachmentType = "ProfileImage",
                ContactId = Guid.NewGuid()
            };

            // Act
            var dto = attachment.ToDto();

            // Assert
            Assert.Equal(EntityTypes.Person, dto.EntityType);
        }
    }
}
