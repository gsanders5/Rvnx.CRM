using System.Globalization;

namespace Rvnx.CRM.Web.Extensions;

public static class EventDateDisplayExtensions
{
    /// <summary>
    /// Formats a significant date's EventDate for display. Year 0001 is the
    /// "year unknown" sentinel — those dates show month and day only.
    /// </summary>
    public static string ToEventDateDisplay(this DateOnly date) =>
        date.Year == 1
            ? date.ToString("M/d", CultureInfo.CurrentCulture)
            : date.ToShortDateString();
}
