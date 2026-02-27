using Rvnx.CRM.Core.Services;

namespace Rvnx.CRM.Tests.Services
{
    public partial class FileValidationServiceTests
    {
        private readonly FileValidationService _service;

        public FileValidationServiceTests()
        {
            _service = new FileValidationService();
        }

        [Theory]
        [InlineData(".jpg")]
        [InlineData(".jpeg")]
        [InlineData(".png")]
        [InlineData(".gif")]
        [InlineData(".pdf")]
        [InlineData(".txt")]
        [InlineData(".doc")]
        [InlineData(".docx")]
        [InlineData(".xls")]
        [InlineData(".xlsx")]
        [InlineData(".JPG")] // Case insensitive
        [InlineData(".PDF")]
        public void IsAllowedExtensionShouldReturnTrueForAllowedExtensions(string extension)
        {
            Assert.True(_service.IsAllowedExtension(extension));
        }

        [Theory]
        [InlineData(".exe")]
        [InlineData(".bat")]
        [InlineData(".sh")]
        [InlineData(".ppt")]
        [InlineData(".pptx")]
        [InlineData("")]
        [InlineData(null)]
        public void IsAllowedExtensionShouldReturnFalseForDisallowedExtensions(string? extension)
        {
            Assert.False(_service.IsAllowedExtension(extension!));
        }

        [Fact]
        public void IsAllowedFileSizeShouldReturnTrueForValidSize()
        {
            long validSize = 30 * 1024 * 1024; // 30 MB
            Assert.True(_service.IsAllowedFileSize(validSize));
            Assert.True(_service.IsAllowedFileSize(1));
        }

        [Fact]
        public void IsAllowedFileSizeShouldReturnFalseForTooLargeSize()
        {
            long invalidSize = (30 * 1024 * 1024) + 1; // 30 MB + 1 byte
            Assert.False(_service.IsAllowedFileSize(invalidSize));
        }

        [Fact]
        public void IsAllowedFileSizeShouldReturnFalseForZeroOrNegativeSize()
        {
            Assert.False(_service.IsAllowedFileSize(0));
            Assert.False(_service.IsAllowedFileSize(-1));
        }

        [Theory]
        [InlineData(".jpg")]
        [InlineData(".jpeg")]
        [InlineData(".png")]
        [InlineData(".gif")]
        [InlineData(".JPG")]
        public void IsImageExtensionShouldReturnTrueForValidExtensions(string extension)
        {
            Assert.True(_service.IsImageExtension(extension));
        }

        [Theory]
        [InlineData(".txt")]
        [InlineData(".exe")]
        [InlineData(".php")]
        [InlineData("")]
        [InlineData(null)]
        public void IsImageExtensionShouldReturnFalseForInvalidExtensions(string? extension)
        {
            Assert.False(_service.IsImageExtension(extension));
        }

        [Fact]
        public void IsValidImageSignatureShouldReturnTrueForValidPng()
        {
            byte[] pngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            Assert.True(_service.IsValidImageSignature(pngSignature, ".png"));
        }

        [Fact]
        public void IsValidImageSignatureShouldReturnTrueForValidJpg()
        {
            byte[] jpgSignature = { 0xFF, 0xD8, 0xFF, 0xE0 };
            Assert.True(_service.IsValidImageSignature(jpgSignature, ".jpg"));
        }

        [Fact]
        public void IsValidImageSignatureShouldReturnFalseForInvalidSignature()
        {
            byte[] invalidSignature = { 0x00, 0x00, 0x00, 0x00 };
            Assert.False(_service.IsValidImageSignature(invalidSignature, ".png"));
        }

        [Fact]
        public void IsValidImageSignatureShouldReturnFalseForShortFile()
        {
            byte[] shortFile = { 0xFF, 0xD8 };
            Assert.False(_service.IsValidImageSignature(shortFile, ".jpg"));
        }

        [Fact]
        public void IsValidImageSignatureShouldHandleCaseInsensitiveExtension()
        {
            byte[] pngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            Assert.True(_service.IsValidImageSignature(pngSignature, ".PNG"));
        }

        [Fact]
        public void IsValidImageSignatureShouldReturnFalseWhenFileBytesIsNull()
        {
            Assert.False(_service.IsValidImageSignature(null!, ".png"));
        }
    }
}
namespace Rvnx.CRM.Tests.Services
{
    public partial class FileValidationServiceTests
    {
        [Fact]
        public void IsValidFileSignatureShouldReturnTrueForValidPdf()
        {
            byte[] bytes = { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
            Assert.True(_service.IsValidFileSignature(bytes, ".pdf"));
        }

        [Fact]
        public void IsValidFileSignatureShouldReturnFalseForInvalidPdf()
        {
            byte[] bytes = { 0x00, 0x00, 0x00, 0x00, 0x00 };
            Assert.False(_service.IsValidFileSignature(bytes, ".pdf"));
        }

        [Fact]
        public void IsValidFileSignatureShouldReturnTrueForValidDocx()
        {
            byte[] bytes = new byte[20];
            bytes[0] = 0x50;
            bytes[1] = 0x4B;
            bytes[2] = 0x03;
            bytes[3] = 0x04;
            Assert.True(_service.IsValidFileSignature(bytes, ".docx"));
        }

        [Fact]
        public void IsValidFileSignatureShouldReturnTrueForValidDoc()
        {
            byte[] bytes = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };
            Assert.True(_service.IsValidFileSignature(bytes, ".doc"));
        }

        [Fact]
        public void IsValidFileSignatureShouldReturnTrueForTxt()
        {
            byte[] bytes = { 0x00, 0x01, 0x02, 0x03, 0x04 }; // Anything
            Assert.True(_service.IsValidFileSignature(bytes, ".txt"));
        }

        [Fact]
        public void IsValidFileSignatureShouldReturnFalseForUnknownExtension()
        {
            byte[] bytes = { 0x00, 0x00, 0x00, 0x00 };
            Assert.False(_service.IsValidFileSignature(bytes, ".unknown"));
        }

        [Fact]
        public void IsValidFileSignatureShouldReturnTrueForValidImage()
        {
            byte[] bytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            Assert.True(_service.IsValidFileSignature(bytes, ".png"));
        }

        [Fact]
        public void IsValidFileSignatureShouldReturnFalseWhenFileBytesIsNull()
        {
            Assert.False(_service.IsValidFileSignature(null!, ".pdf"));
        }
    }
}
