namespace Rvnx.CRM.Core.Services
{
    public static class DateCalculationService
    {
        public static DateOnly GetNextOccurrence(Rvnx.CRM.Core.Models.Dates.SignificantDate significantDate, DateOnly fromDate)
        {
            if (significantDate.EventDate > fromDate)
            {
                return significantDate.EventDate;
            }

            if (significantDate.RecurrenceType == Enumerations.RecurrenceType.None)
            {
                return significantDate.EventDate;
            }

            if (significantDate.RecurrenceType == Enumerations.RecurrenceType.Annual)
            {
                int year = fromDate.Year;
                int month = significantDate.EventDate.Month;
                int day = Math.Min(significantDate.EventDate.Day, DateTime.DaysInMonth(year, month));
                DateOnly nextOccurrence = new DateOnly(year, month, day);

                if (nextOccurrence < fromDate)
                {
                    year++;
                    day = Math.Min(significantDate.EventDate.Day, DateTime.DaysInMonth(year, month));
                    nextOccurrence = new DateOnly(year, month, day);
                }
                return nextOccurrence;
            }

            if (significantDate.RecurrenceType == Enumerations.RecurrenceType.Monthly)
            {
                DateOnly nextOccurrence = significantDate.EventDate;

                int monthDiff = ((fromDate.Year - significantDate.EventDate.Year) * 12) + fromDate.Month - significantDate.EventDate.Month;
                if (monthDiff > 0)
                {
                    nextOccurrence = significantDate.EventDate.AddMonths(monthDiff);
                }

                if (nextOccurrence < fromDate)
                {
                    nextOccurrence = nextOccurrence.AddMonths(1);
                }

                return nextOccurrence;
            }

            if (significantDate.RecurrenceType == Enumerations.RecurrenceType.Custom && significantDate.CustomIntervalDays.HasValue && significantDate.CustomIntervalDays.Value > 0)
            {
                DateOnly nextOccurrence = significantDate.EventDate;
                int interval = significantDate.CustomIntervalDays.Value;

                int daysDiff = fromDate.DayNumber - significantDate.EventDate.DayNumber;

                if (daysDiff > 0)
                {
                    int cycles = daysDiff / interval;
                    if (cycles > 0)
                    {
                        nextOccurrence = nextOccurrence.AddDays(cycles * interval);
                    }
                }

                while (nextOccurrence < fromDate)
                {
                    nextOccurrence = nextOccurrence.AddDays(interval);
                }

                return nextOccurrence;
            }

            return significantDate.EventDate;
        }

        public static DateOnly GetScheduledForDate(Rvnx.CRM.Core.Models.Dates.SignificantDate significantDate, Rvnx.CRM.Core.Models.Dates.ReminderOffset offset, DateOnly fromDate)
        {
            DateOnly nextOccurrence = GetNextOccurrence(significantDate, fromDate);
            return nextOccurrence.AddDays(-offset.DaysBeforeEvent);
        }
    }
}
