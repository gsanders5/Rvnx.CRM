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

        string message = error ?? PhoneNumberNormalizer.InvalidPhoneMessage;
        return validationContext.MemberName is { } memberName
            ? new ValidationResult(message, [memberName])
            : new ValidationResult(message);
    }
}
