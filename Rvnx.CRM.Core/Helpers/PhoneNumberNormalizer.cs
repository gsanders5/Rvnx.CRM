using PhoneNumbers;
using Rvnx.CRM.Core.Enumerations;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Rvnx.CRM.Core.Helpers;

public static class PhoneNumberNormalizer
{
    public const string DefaultRegion = "US";

    public const string InvalidPhoneMessage =
        "Enter a valid phone number, e.g. (555) 123-4567 or +44 20 7946 0958. Extensions like 'ext 123' are allowed.";

    private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

    public static bool TryNormalize(string? value, out string normalized, out string? error, string defaultRegion = DefaultRegion)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            normalized = value ?? string.Empty;
            error = null;
            return true;
        }

        string trimmed = value.Trim();

        try
        {
            PhoneNumber parsed = Util.Parse(trimmed, defaultRegion);
            if (!Util.IsValidNumber(parsed))
            {
                normalized = value;
                error = InvalidPhoneMessage;
                return false;
            }

            string e164 = Util.Format(parsed, PhoneNumberFormat.E164);
            normalized = parsed.HasExtension
                ? $"{e164};ext={parsed.Extension}"
                : e164;
            error = null;
            return true;
        }
        catch (NumberParseException)
        {
            normalized = value;
            error = InvalidPhoneMessage;
            return false;
        }
    }

    public static string NormalizeOrThrow(ContactMethodType type, string? value, string defaultRegion = DefaultRegion)
    {
        if (type != ContactMethodType.Phone)
        {
            return value ?? string.Empty;
        }

        return TryNormalize(value, out string normalized, out string? error, defaultRegion)
            ? normalized
            : throw new ValidationException(error);
    }

    public static string FormatForDisplay(string? stored, string defaultRegion = DefaultRegion)
    {
        if (!TryParseValid(stored, defaultRegion, out PhoneNumber? parsed))
        {
            return stored ?? string.Empty;
        }

        int defaultCountryCode = Util.GetCountryCodeForRegion(defaultRegion);
        PhoneNumberFormat format = parsed.CountryCode == defaultCountryCode
            ? PhoneNumberFormat.NATIONAL
            : PhoneNumberFormat.INTERNATIONAL;

        return Util.Format(parsed, format);
    }

    public static string FormatForTelUri(string? stored, string defaultRegion = DefaultRegion)
    {
        return TryParseValid(stored, defaultRegion, out PhoneNumber? parsed)
            ? Util.Format(parsed, PhoneNumberFormat.RFC3966)
            : stored ?? string.Empty;
    }

    private static bool TryParseValid(string? stored, string defaultRegion, [NotNullWhen(true)] out PhoneNumber? parsed)
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(stored))
        {
            return false;
        }

        try
        {
            PhoneNumber candidate = Util.Parse(stored, defaultRegion);
            if (!Util.IsValidNumber(candidate))
            {
                return false;
            }

            parsed = candidate;
            return true;
        }
        catch (NumberParseException)
        {
            return false;
        }
    }
}
