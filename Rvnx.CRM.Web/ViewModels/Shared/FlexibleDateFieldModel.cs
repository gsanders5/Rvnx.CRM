// CA1716: "Shared" is a VB reserved keyword, but the namespace intentionally mirrors
// the conventional Views/Shared folder; VB consumers are not a concern for this web app.
#pragma warning disable CA1716
namespace Rvnx.CRM.Web.ViewModels.Shared;
#pragma warning restore CA1716

/// <summary>
/// Drives Views/Shared/_FlexibleDateField.cshtml: a month / day / year select
/// trio for dates whose year may be unknown (birthdays, anniversaries). The
/// year select's first option is "Year unknown", which posts the year 0001
/// sentinel; an empty month or day posts an empty value.
/// </summary>
public class FlexibleDateFieldModel
{
    /// <summary>Form field name the canonical yyyy-MM-dd value posts under, e.g. "Birthday".</summary>
    public required string Name { get; init; }

    public required string Label { get; init; }

    /// <summary>Current value; year 0001 means the year is unknown, null means no date.</summary>
    public DateOnly? Value { get; init; }
}
