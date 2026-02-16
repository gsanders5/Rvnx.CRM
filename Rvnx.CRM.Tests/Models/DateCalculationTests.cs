using System;
using Xunit;
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
        public void StandardBirthday_ReturnsNextYear_WhenPast()
        {
            // Arrange
            var birthday = new SignificantDate
            {
                Date = new DateTime(2020, 2, 14), // Feb 14
                EventFrequency = TimeSpan.FromDays(365)
            };

            // Act
            // We use the Service directly to mock "Today" easily, or we rely on the Model using DateTime.Today
            // Since Model uses DateTime.Today, we should test the Service logic primarily if we want to inject 'Today'
            // OR we can just use the service directly for these tests as requested "Can you add a test ... for these computed dates?"

            var nextOccurrence = DateCalculationService.GetNextOccurrence(birthday.Date, birthday.EventFrequency, _today);

            // Assert
            Assert.Equal(new DateTime(2027, 2, 14), nextOccurrence);
        }

        [Fact]
        public void StandardBirthday_ReturnsThisYear_WhenFuture()
        {
            // Arrange
            var birthday = new DateTime(2020, 2, 16); // Feb 16
            var freq = TimeSpan.FromDays(365);

            // Act
            var nextOccurrence = DateCalculationService.GetNextOccurrence(birthday, freq, _today);

            // Assert
            Assert.Equal(new DateTime(2026, 2, 16), nextOccurrence);
        }

        [Fact]
        public void StandardBirthday_ReturnsToday_WhenToday()
        {
            // Arrange
            var birthday = new DateTime(2020, 2, 15); // Feb 15
            var freq = TimeSpan.FromDays(365);

            // Act
            var nextOccurrence = DateCalculationService.GetNextOccurrence(birthday, freq, _today);

            // Assert
            Assert.Equal(_today, nextOccurrence);
        }

        [Fact]
        public void LeapYearBirthday_ReturnsFeb28_OnNonLeapYear()
        {
            // Arrange
            var birthday = new DateTime(2020, 2, 29); // Leap Year
            var freq = TimeSpan.FromDays(365);
            // 2026 is not a leap year

            // Act
            var nextOccurrence = DateCalculationService.GetNextOccurrence(birthday, freq, _today);

            // Assert
            Assert.Equal(new DateTime(2026, 2, 28), nextOccurrence);
        }

        [Fact]
        public void LeapYearBirthday_ReturnsFeb29_OnFutureLeapYear()
        {
            // Arrange
            var birthday = new DateTime(2020, 2, 29);
            var freq = TimeSpan.FromDays(365);
            var todayIn2027 = new DateTime(2027, 3, 1); // Past Feb 2027

            // Act
            // Next leap year is 2028
            var nextOccurrence = DateCalculationService.GetNextOccurrence(birthday, freq, todayIn2027);

            // Assert
            Assert.Equal(new DateTime(2028, 2, 29), nextOccurrence);
        }

        [Fact]
        public void DriftLogic_DriftsDates()
        {
            // Arrange
            var start = new DateTime(2026, 1, 1);
            var freq = TimeSpan.FromDays(30);
            // Today is 2026-02-15
            // 1: 01-01
            // 2: 01-31
            // 3: 03-02 (2026 is non-leap)

            // Act
            var nextOccurrence = DateCalculationService.GetNextOccurrence(start, freq, _today);

            // Assert
            // Should skip 1/1 and 1/31 because they are < today (2/15)
            // Should return 3/2
            Assert.Equal(new DateTime(2026, 3, 2), nextOccurrence);
        }

        [Fact]
        public void DriftLogic_ReturnsToday_WhenFallsOnToday()
        {
            // Arrange
            var start = new DateTime(2026, 1, 16); // 1 month before today (approx)
            var freq = TimeSpan.FromDays(30);
            // 1/16 + 30 days = 2/15 (Today)

            // Act
            var nextOccurrence = DateCalculationService.GetNextOccurrence(start, freq, _today);

            // Assert
            Assert.Equal(_today, nextOccurrence);
        }

        [Fact]
        public void OneOffReminder_ReturnsOriginalDate_WhenPast()
        {
            // Arrange
            // Due date in past, no frequency (one-off)
            var dueDate = new DateTime(2025, 1, 1);
            var freq = TimeSpan.Zero;

            // Act
            var nextOccurrence = DateCalculationService.GetNextOccurrence(dueDate, freq, _today);

            // Assert
            // Should return original date (overdue), not recur
            Assert.Equal(dueDate, nextOccurrence);
        }
    }
}
