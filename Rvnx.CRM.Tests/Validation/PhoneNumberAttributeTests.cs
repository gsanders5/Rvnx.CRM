using Rvnx.CRM.Core.Helpers;
using Rvnx.CRM.Core.Validation;
using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Tests.Validation;

public class PhoneNumberAttributeTests
{
    private static ValidationResult? Validate(object? value)
    {
        PhoneNumberAttribute attribute = new();
        ValidationContext context = new(new object()) { MemberName = "Phone" };
        return attribute.GetValidationResult(value, context);
    }

    [Theory]
    [InlineData("(212) 736-5000")]
    [InlineData("+44 20 7946 0958")]
    [InlineData("212-736-5000 ext. 890")]
    public void ValidateWhenValidReturnsSuccess(string input)
    {
        ValidationResult? result = Validate(input);

        Assert.Equal(ValidationResult.Success, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateWhenEmptyReturnsSuccess(string? input)
    {
        ValidationResult? result = Validate(input);

        Assert.Equal(ValidationResult.Success, result);
    }

    [Theory]
    [InlineData("not a phone")]
    [InlineData("123")]
    public void ValidateWhenInvalidReturnsErrorMessage(string input)
    {
        ValidationResult? result = Validate(input);

        Assert.NotNull(result);
        Assert.Equal(PhoneNumberNormalizer.InvalidPhoneMessage, result!.ErrorMessage);
        Assert.Contains("Phone", result.MemberNames);
    }
}
