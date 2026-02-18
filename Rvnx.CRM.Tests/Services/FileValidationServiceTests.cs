using Rvnx.CRM.Core.Services;
using Xunit;

namespace Rvnx.CRM.Tests.Services
{
    public class FileValidationServiceTests
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
        [InlineData(".JPG")]
        public void IsImageExtension_ShouldReturnTrue_ForValidExtensions(string extension)
        {
            Assert.True(_service.IsImageExtension(extension));
        }

        [Theory]
        [InlineData(".txt")]
        [InlineData(".exe")]
        [InlineData(".php")]
        [InlineData("")]
        [InlineData(null)]
        public void IsImageExtension_ShouldReturnFalse_ForInvalidExtensions(string extension)
        {
            Assert.False(_service.IsImageExtension(extension));
        }

        [Fact]
        public void IsValidImageSignature_ShouldReturnTrue_ForValidPng()
        {
            byte[] pngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            Assert.True(_service.IsValidImageSignature(pngSignature, ".png"));
        }

        [Fact]
        public void IsValidImageSignature_ShouldReturnTrue_ForValidJpg()
        {
            byte[] jpgSignature = { 0xFF, 0xD8, 0xFF, 0xE0 };
            Assert.True(_service.IsValidImageSignature(jpgSignature, ".jpg"));
        }

        [Fact]
        public void IsValidImageSignature_ShouldReturnFalse_ForInvalidSignature()
        {
            byte[] invalidSignature = { 0x00, 0x00, 0x00, 0x00 };
            Assert.False(_service.IsValidImageSignature(invalidSignature, ".png"));
        }

        [Fact]
        public void IsValidImageSignature_ShouldReturnFalse_ForShortFile()
        {
            byte[] shortFile = { 0xFF, 0xD8 };
            Assert.False(_service.IsValidImageSignature(shortFile, ".jpg"));
        }

        [Fact]
        public void IsValidImageSignature_ShouldHandleCaseInsensitiveExtension()
        {
            byte[] pngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            Assert.True(_service.IsValidImageSignature(pngSignature, ".PNG"));
        }
    }
}
