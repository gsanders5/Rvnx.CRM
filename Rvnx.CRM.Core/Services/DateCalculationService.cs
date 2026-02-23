namespace Rvnx.CRM.Core.Services
{
    public static class DateCalculationService
    {
        public static DateTime GetNextOccurrence(DateTime originalDate, TimeSpan frequency, DateTime? referenceDate = null)
        {
            DateTime today = referenceDate ?? DateTime.Today;
            DateTime nextOccurrence = originalDate;

            if (nextOccurrence > today)
            {
                return nextOccurrence;
            }

            if (frequency <= TimeSpan.Zero)
            {
                // If no frequency, it's a one-time event (or already happened)
                // Just return the original date
                return nextOccurrence;
            }

            bool isMultipleOf365Days = (frequency.TotalDays % 365) == 0;

            if (isMultipleOf365Days)
            {
                int yearsPerCycle = (int)(frequency.TotalDays / 365);
                int yearsSinceOriginal = today.Year - originalDate.Year;
                int yearsToAdd = 0;

                if (yearsSinceOriginal > 0 && yearsPerCycle > 0)
                {
                    int cycles = yearsSinceOriginal / yearsPerCycle;
                    if (cycles > 0)
                    {
                        yearsToAdd = cycles * yearsPerCycle;
                    }
                }

                nextOccurrence = originalDate.AddYears(yearsToAdd);

                while (nextOccurrence < today)
                {
                    yearsToAdd += yearsPerCycle;
                    nextOccurrence = originalDate.AddYears(yearsToAdd);
                }
            }
            else
            {
                double daysSinceOriginal = (today - nextOccurrence).TotalDays;

                if (daysSinceOriginal > 0 && frequency.TotalDays > 0)
                {
                    double cycles = Math.Floor(daysSinceOriginal / frequency.TotalDays);
                    if (cycles > 0)
                    {
                        nextOccurrence = nextOccurrence.Add(frequency * cycles);
                    }
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
