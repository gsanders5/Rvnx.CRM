using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;

namespace Rvnx.CRM.Tests.Models
{
    public class DateCalculationTests
    {
        private readonly DateTime _today;

        public DateCalculationTests()
        {
            // Simulate today as 2026-02-15 for consistency with previous checks
            _today = new DateTime(2026, 2, 15);
        }

        [Fact]
        public void StandardBirthdayReturnsNextYearWhenPast()
        {
            SignificantDate birthday = new()
            {
                Date = new DateTime(2020, 2, 14), // Feb 14
                EventFrequency = TimeSpan.FromDays(365)
            };

            // We use the Service directly to mock "Today" easily, or we rely on the Model using DateTime.Today
            // Since Model uses DateTime.Today, we should test the Service logic primarily if we want to inject 'Today'
            // OR we can just use the service directly for these tests as requested "Can you add a test ... for these computed dates?"

            DateTime nextOccurrence = DateCalculationService.GetNextOccurrence(birthday.Date, birthday.EventFrequency, _today, treatFrequencyAsCalendarYears: true);

            Assert.Equal(new DateTime(2027, 2, 14), nextOccurrence);
        }

        [Fact]
        public void StandardBirthdayReturnsThisYearWhenFuture()
        {
            DateTime birthday = new(2020, 2, 16); // Feb 16
            TimeSpan freq = TimeSpan.FromDays(365);

            DateTime nextOccurrence = DateCalculationService.GetNextOccurrence(birthday, freq, _today, treatFrequencyAsCalendarYears: true);

            Assert.Equal(new DateTime(2026, 2, 16), nextOccurrence);
        }

        [Fact]
        public void StandardBirthdayReturnsTodayWhenToday()
        {
            DateTime birthday = new(2020, 2, 15); // Feb 15
            TimeSpan freq = TimeSpan.FromDays(365);

            DateTime nextOccurrence = DateCalculationService.GetNextOccurrence(birthday, freq, _today, treatFrequencyAsCalendarYears: true);

            Assert.Equal(_today, nextOccurrence);
        }

        [Fact]
        public void LeapYearBirthdayReturnsFeb28OnNonLeapYear()
        {
            DateTime birthday = new(2020, 2, 29); // Leap Year
            TimeSpan freq = TimeSpan.FromDays(365);
            // 2026 is not a leap year

            DateTime nextOccurrence = DateCalculationService.GetNextOccurrence(birthday, freq, _today, treatFrequencyAsCalendarYears: true);

            Assert.Equal(new DateTime(2026, 2, 28), nextOccurrence);
        }

        [Fact]
        public void LeapYearBirthdayReturnsFeb29OnFutureLeapYear()
        {
            DateTime birthday = new(2020, 2, 29);
            TimeSpan freq = TimeSpan.FromDays(365);
            DateTime todayIn2027 = new(2027, 3, 1); // Past Feb 2027

            // Next leap year is 2028
            DateTime nextOccurrence = DateCalculationService.GetNextOccurrence(birthday, freq, todayIn2027, treatFrequencyAsCalendarYears: true);

            Assert.Equal(new DateTime(2028, 2, 29), nextOccurrence);
        }

        [Fact]
        public void DriftLogicDriftsDates()
        {
            DateTime start = new(2026, 1, 1);
            TimeSpan freq = TimeSpan.FromDays(30);

            DateTime nextOccurrence = DateCalculationService.GetNextOccurrence(start, freq, _today);

            // Should skip 1/1 and 1/31 because they are < today (2/15)
            Assert.Equal(new DateTime(2026, 3, 2), nextOccurrence);
        }

        [Fact]
        public void DriftLogicReturnsTodayWhenFallsOnToday()
        {
            DateTime start = new(2026, 1, 16); // 1 month before today (approx)
            TimeSpan freq = TimeSpan.FromDays(30);
            // 1/16 + 30 days = 2/15 (Today)

            DateTime nextOccurrence = DateCalculationService.GetNextOccurrence(start, freq, _today);

            Assert.Equal(_today, nextOccurrence);
        }

        [Fact]
        public void OneOffReminderReturnsOriginalDateWhenPast()
        {
            // Due date in past, no frequency (one-off)
            DateTime dueDate = new(2025, 1, 1);
            TimeSpan freq = TimeSpan.Zero;

            DateTime nextOccurrence = DateCalculationService.GetNextOccurrence(dueDate, freq, _today);

            // Should return original date (overdue), not recur
            Assert.Equal(dueDate, nextOccurrence);
        }
    }
}
