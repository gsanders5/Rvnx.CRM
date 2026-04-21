using Rvnx.CRM.Core.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class PhoneNumberAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return ValidationResult.Success;
        }

        if (PhoneNumberNormalizer.TryNormalize(text, out _, out string? error))
        {
            return ValidationResult.Success;
        }

        string[]? memberNames = validationContext.MemberName is null
            ? null
            : [validationContext.MemberName];
        return new ValidationResult(error ?? PhoneNumberNormalizer.InvalidPhoneMessage, memberNames);
    }
}
