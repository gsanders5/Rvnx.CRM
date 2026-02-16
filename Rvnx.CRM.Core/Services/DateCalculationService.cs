using System;

namespace Rvnx.CRM.Core.Services
{
    public static class DateCalculationService
    {
        public static DateTime GetNextOccurrence(DateTime originalDate, TimeSpan frequency, DateTime? referenceDate = null)
        {
            DateTime today = referenceDate ?? DateTime.Today;
            DateTime nextOccurrence = originalDate;

            // If date is already in future, just return it
            if (nextOccurrence > today) return nextOccurrence;

            // Default frequency to Yearly if <= Zero
            if (frequency <= TimeSpan.Zero) frequency = TimeSpan.FromDays(365);

            // Check if strict yearly multiple (multiple of 365 days)
            // Note: 365 days is approx 1 year. The user requested strict calendar logic for 365 days.
            bool isStrictYearly = (frequency.TotalDays % 365) == 0;

            if (isStrictYearly)
            {
                int yearsPerCycle = (int)(frequency.TotalDays / 365);

                // Calculate years difference to jump ahead efficiently instead of looping 100 times for a 1920 birthday
                int yearDiff = today.Year - originalDate.Year;
                int totalYearsToAdd = 0;

                if (yearDiff > 0 && yearsPerCycle > 0)
                {
                    // Add enough cycles to get close to current year
                    int cycles = yearDiff / yearsPerCycle;
                    if (cycles > 0)
                        totalYearsToAdd = cycles * yearsPerCycle;
                }

                // Apply directly to base date to preserve 2/29 logic
                // C# AddYears on 2/29 automatically returns 2/28 on non-leap years, and 2/29 on leap years
                nextOccurrence = originalDate.AddYears(totalYearsToAdd);

                // Fine tune
                while (nextOccurrence < today)
                {
                     totalYearsToAdd += yearsPerCycle;
                     nextOccurrence = originalDate.AddYears(totalYearsToAdd);
                }
            }
            else
            {
                // Drift logic: Add TimeSpan repeatedly
                // To avoid massive loops for old dates with small frequencies (e.g. 1990 date with 1 day freq)
                // Calculate total days difference
                double daysDiff = (today - nextOccurrence).TotalDays;
                if (daysDiff > 0 && frequency.TotalDays > 0)
                {
                     double cycles = Math.Floor(daysDiff / frequency.TotalDays);
                     if (cycles > 0)
                        nextOccurrence = nextOccurrence.Add(frequency * cycles);
                }

                while (nextOccurrence < today)
                {
                    nextOccurrence = nextOccurrence.Add(frequency);
                }
            }

            return nextOccurrence;
        }
    }
}
