using Rvnx.CRM.Core.Services;

namespace Rvnx.CRM.Tests.Services
{
    public class DateCalculationServiceTests
    {
        [Fact]
        public void GetNextOccurrenceSevenDaysAddsSevenDays()
        {
            DateTime start = new(2023, 1, 1);
            TimeSpan frequency = TimeSpan.FromDays(7);
            DateTime today = new(2023, 1, 2);

            DateTime result = DateCalculationService.GetNextOccurrence(start, frequency, today);

            Assert.Equal(new DateTime(2023, 1, 8), result);
        }

        [Fact]
        public void GetNextOccurrence365DaysTreatsAsOneYear()
        {
            // 2024 is a leap year (366 days).
            // If we use strictly 365 days logic, 2023-01-01 + 365 days = 2024-01-01.
            // 2024-01-01 + 365 days = 2024-12-31 (because 2024 has 366 days).
            // BUT the service logic treats 365 days as AddYears(1).
            // So 2024-01-01 + 1 year = 2025-01-01.
            DateTime start = new(2024, 1, 1);
            TimeSpan frequency = TimeSpan.FromDays(365);
            DateTime today = new(2024, 6, 1);

            DateTime result = DateCalculationService.GetNextOccurrence(start, frequency, today, treatFrequencyAsCalendarYears: true);

            Assert.Equal(new DateTime(2025, 1, 1), result);
        }

        [Fact]
        public void GetNextOccurrenceLeapYearPreservesFeb29()
        {
            DateTime start = new(2020, 2, 29); // Leap day
            TimeSpan frequency = TimeSpan.FromDays(365); // "Yearly"
            DateTime today = new(2021, 3, 1);

            DateTime result = DateCalculationService.GetNextOccurrence(start, frequency, today, treatFrequencyAsCalendarYears: true);

            Assert.Equal(new DateTime(2022, 2, 28), result);
        }

        [Fact]
        public void GetNextOccurrenceLeapYearReturnsToFeb29OnNextLeapYear()
        {
            DateTime start = new(2020, 2, 29);
            TimeSpan frequency = TimeSpan.FromDays(365);
            DateTime today = new(2023, 3, 1); // After Feb 2023

            DateTime result = DateCalculationService.GetNextOccurrence(start, frequency, today, treatFrequencyAsCalendarYears: true);

            // 2020 -> 2021 (Feb 28) -> 2022 (Feb 28) -> 2023 (Feb 28).
            // Next is 2024 (Leap Year). Should match Feb 29.
            Assert.Equal(new DateTime(2024, 2, 29), result);
        }

        [Fact]
        public void GetNextOccurrenceFarPastDateCatchesUp()
        {
            DateTime start = new(2000, 1, 1);
            TimeSpan frequency = TimeSpan.FromDays(365);
            DateTime today = new(2023, 6, 1);

            DateTime result = DateCalculationService.GetNextOccurrence(start, frequency, today, treatFrequencyAsCalendarYears: true);

            // Should be next Jan 1 after June 2023
            Assert.Equal(new DateTime(2024, 1, 1), result);
        }

        [Fact]
        public void GetNextOccurrenceZeroFrequencyReturnsOriginalDate()
        {
            DateTime start = new(2023, 1, 1);
            TimeSpan frequency = TimeSpan.Zero;
            DateTime today = new(2023, 6, 1);

            DateTime result = DateCalculationService.GetNextOccurrence(start, frequency, today);

            // Even though it's in the past, zero frequency means no recurrence.
            Assert.Equal(start, result);
        }

        [Fact]
        public void GetNextOccurrenceDueTodayReturnsToday()
        {
            DateTime start = new(2023, 1, 1);
            TimeSpan frequency = TimeSpan.FromDays(1);
            DateTime today = new(2023, 1, 5);

            DateTime result = DateCalculationService.GetNextOccurrence(start, frequency, today);

            Assert.Equal(today, result);
        }

        [Fact]
        public void GetNextOccurrenceStandardIntervalDriftsCorrectly()
        {
            DateTime start = new(2023, 1, 1);
            TimeSpan frequency = TimeSpan.FromDays(30);
            DateTime today = new(2023, 2, 1); // 31 days later

            DateTime result = DateCalculationService.GetNextOccurrence(start, frequency, today);

            // Jan 1 + 30 days = Jan 31.
            // Jan 31 is not > today (Feb 1). Loop continues.
            // Jan 31 + 30 days = Mar 2 (2023 non-leap: Jan 31 + 28 = Feb 28 + 2 = Mar 2).
            Assert.Equal(new DateTime(2023, 3, 2), result);
        }

        [Fact]
        public void GetNextOccurrenceFutureDateReturnsOriginal()
        {
            DateTime start = new(2025, 1, 1);
            TimeSpan frequency = TimeSpan.FromDays(1);
            DateTime today = new(2023, 1, 1);

            DateTime result = DateCalculationService.GetNextOccurrence(start, frequency, today);

            Assert.Equal(start, result);
        }

        [Fact]
        public void GetNextOccurrenceMultipleOf365ButNotOneYearCalculatesCorrectly()
        {
            DateTime start = new(2020, 1, 1);
            TimeSpan frequency = TimeSpan.FromDays(730); // 2 years
            DateTime today = new(2021, 1, 1);

            DateTime result = DateCalculationService.GetNextOccurrence(start, frequency, today, treatFrequencyAsCalendarYears: true);

            Assert.Equal(new DateTime(2022, 1, 1), result);
        }

        [Fact]
        public void GetNextOccurrenceLeapYearIntervalDriftsOneDayPerYear()
        {
            DateTime start = new(2021, 1, 1);
            TimeSpan frequency = TimeSpan.FromDays(366); // Leap year length
            DateTime today = new(2023, 1, 1);

            DateTime result = DateCalculationService.GetNextOccurrence(start, frequency, today);

            Assert.Equal(new DateTime(2023, 1, 3), result);
        }
    }
}
