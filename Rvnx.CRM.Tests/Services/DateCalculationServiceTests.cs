using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;

namespace Rvnx.CRM.Tests.Services
{
    public class DateCalculationServiceTests
    {
        [Fact]
        public void GetNextOccurrenceAnnualSameYearWhenDateNotYetPassed()
        {
            var significantDate = new SignificantDate
            {
                EventDate = new DateOnly(2020, 5, 15),
                RecurrenceType = RecurrenceType.Annual
            };
            var today = new DateOnly(2023, 2, 1);

            var result = DateCalculationService.GetNextOccurrence(significantDate, today);

            Assert.Equal(new DateOnly(2023, 5, 15), result);
        }

        [Fact]
        public void GetNextOccurrenceAnnualRollsToNextYearWhenDateHasPassed()
        {
            var significantDate = new SignificantDate
            {
                EventDate = new DateOnly(2020, 5, 15),
                RecurrenceType = RecurrenceType.Annual
            };
            var today = new DateOnly(2023, 6, 1);

            var result = DateCalculationService.GetNextOccurrence(significantDate, today);

            Assert.Equal(new DateOnly(2024, 5, 15), result);
        }

        [Fact]
        public void GetNextOccurrenceAnnualFeb29ReturnsFeb28InNonLeapYear()
        {
            var significantDate = new SignificantDate
            {
                EventDate = new DateOnly(2020, 2, 29),
                RecurrenceType = RecurrenceType.Annual
            };
            var today = new DateOnly(2023, 1, 1); // 2023 is not a leap year

            var result = DateCalculationService.GetNextOccurrence(significantDate, today);

            Assert.Equal(new DateOnly(2023, 2, 28), result);
        }

        [Fact]
        public void GetNextOccurrenceAnnualFeb29ReturnsFeb29InLeapYear()
        {
            var significantDate = new SignificantDate
            {
                EventDate = new DateOnly(2020, 2, 29),
                RecurrenceType = RecurrenceType.Annual
            };
            var today = new DateOnly(2023, 3, 1); // After Feb 2023. Next is 2024 (Leap year)

            var result = DateCalculationService.GetNextOccurrence(significantDate, today);

            Assert.Equal(new DateOnly(2024, 2, 29), result);
        }

        [Fact]
        public void GetNextOccurrenceNoneAlwaysReturnsFixedEventDate()
        {
            var significantDate = new SignificantDate
            {
                EventDate = new DateOnly(2020, 5, 15),
                RecurrenceType = RecurrenceType.None
            };
            var today = new DateOnly(2023, 6, 1);

            var result = DateCalculationService.GetNextOccurrence(significantDate, today);

            Assert.Equal(new DateOnly(2020, 5, 15), result);
        }

        [Fact]
        public void GetNextOccurrenceMonthlyAdvancesToNextMonthWhenDayHasPassed()
        {
            var significantDate = new SignificantDate
            {
                EventDate = new DateOnly(2023, 1, 15),
                RecurrenceType = RecurrenceType.Monthly
            };
            var today = new DateOnly(2023, 3, 20);

            var result = DateCalculationService.GetNextOccurrence(significantDate, today);

            Assert.Equal(new DateOnly(2023, 4, 15), result);
        }

        [Fact]
        public void GetNextOccurrenceMonthlyClampsToEndOfMonth()
        {
            var significantDate = new SignificantDate
            {
                EventDate = new DateOnly(2023, 1, 31),
                RecurrenceType = RecurrenceType.Monthly
            };
            var today = new DateOnly(2023, 2, 1);

            var result = DateCalculationService.GetNextOccurrence(significantDate, today);

            // Jan 31 advanced 1 month clamps to Feb 28 in 2023.
            Assert.Equal(new DateOnly(2023, 2, 28), result);
        }

        [Fact]
        public void GetNextOccurrenceCustomLandsOnValidIntervalBoundary()
        {
            var significantDate = new SignificantDate
            {
                EventDate = new DateOnly(2023, 1, 1),
                RecurrenceType = RecurrenceType.Custom,
                CustomIntervalDays = 10
            };
            var today = new DateOnly(2023, 1, 15);

            var result = DateCalculationService.GetNextOccurrence(significantDate, today);

            // Start: Jan 1
            // Inter: +10 days -> Jan 11 (Passed)
            // Inter: +10 days -> Jan 21
            Assert.Equal(new DateOnly(2023, 1, 21), result);
        }

        [Fact]
        public void GetScheduledForDateReturnsNextOccurrenceMinusDaysBeforeEvent()
        {
            var significantDate = new SignificantDate
            {
                EventDate = new DateOnly(2023, 5, 15),
                RecurrenceType = RecurrenceType.Annual
            };
            var offset = new ReminderOffset
            {
                DaysBeforeEvent = 7
            };
            var today = new DateOnly(2023, 5, 1);

            // Next occurrence is May 15. Scheduled for should be May 8.
            var result = DateCalculationService.GetScheduledForDate(significantDate, offset, today);

            Assert.Equal(new DateOnly(2023, 5, 8), result);
        }
    }
}
