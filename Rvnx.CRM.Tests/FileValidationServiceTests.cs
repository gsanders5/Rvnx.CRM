using Rvnx.CRM.Core.Services;

namespace Rvnx.CRM.Tests
{
    public class FileValidationServiceTests
    {
        private readonly FileValidationService _service = new();

        [Fact]
        public void IsValidFileSignature_ShouldReturnTrue_ForValidPdf()
        {
            byte[] bytes = { 0x25, 0x50, 0x44, 0x46, 0x00 };
            Assert.True(_service.IsValidFileSignature(bytes, ".pdf"));
        }

        [Fact]
        public void IsValidFileSignature_ShouldReturnFalse_ForInvalidPdf()
        {
            byte[] bytes = { 0x00, 0x00, 0x00, 0x00, 0x00 };
            Assert.False(_service.IsValidFileSignature(bytes, ".pdf"));
        }

        [Fact]
        public void IsValidFileSignature_ShouldReturnTrue_ForValidDocx()
        {
            byte[] bytes = { 0x50, 0x4B, 0x03, 0x04, 0x00 };
            Assert.True(_service.IsValidFileSignature(bytes, ".docx"));
        }

        [Fact]
        public void IsValidFileSignature_ShouldReturnTrue_ForValidDoc()
        {
            byte[] bytes = { 0xD0, 0xCF, 0x11, 0xE0, 0x00 };
            Assert.True(_service.IsValidFileSignature(bytes, ".doc"));
        }

        [Fact]
        public void IsValidFileSignature_ShouldReturnTrue_ForTxt()
        {
            byte[] bytes = { 0x00, 0x01, 0x02, 0x03, 0x04 }; // Anything
            Assert.True(_service.IsValidFileSignature(bytes, ".txt"));
        }

        [Fact]
        public void IsValidFileSignature_ShouldReturnFalse_ForUnknownExtension()
        {
            byte[] bytes = { 0x00, 0x00, 0x00, 0x00 };
            Assert.False(_service.IsValidFileSignature(bytes, ".unknown"));
        }

        [Fact]
        public void IsValidFileSignature_ShouldReturnTrue_ForValidImage()
        {
            // PNG
            byte[] bytes = { 0x89, 0x50, 0x4E, 0x47, 0x00 };
            Assert.True(_service.IsValidFileSignature(bytes, ".png"));
        }
    }
}
