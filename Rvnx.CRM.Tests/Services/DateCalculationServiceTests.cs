using Rvnx.CRM.Core.Services;
using System;
using Xunit;

namespace Rvnx.CRM.Tests.Services
{
    public class DateCalculationServiceTests
    {
        [Fact]
        public void GetNextOccurrence_SevenDays_AddsSevenDays()
        {
            // Arrange
            var start = new DateTime(2023, 1, 1);
            var frequency = TimeSpan.FromDays(7);
            var today = new DateTime(2023, 1, 2);

            // Act
            var result = DateCalculationService.GetNextOccurrence(start, frequency, today);

            // Assert
            Assert.Equal(new DateTime(2023, 1, 8), result);
        }

        [Fact]
        public void GetNextOccurrence_365Days_TreatsAsOneYear()
        {
            // Arrange
            // 2024 is a leap year (366 days).
            // If we use strictly 365 days logic, 2023-01-01 + 365 days = 2024-01-01.
            // 2024-01-01 + 365 days = 2024-12-31 (because 2024 has 366 days).
            // BUT the service logic treats 365 days as AddYears(1).
            // So 2024-01-01 + 1 year = 2025-01-01.
            var start = new DateTime(2024, 1, 1);
            var frequency = TimeSpan.FromDays(365);
            var today = new DateTime(2024, 6, 1);

            // Act
            var result = DateCalculationService.GetNextOccurrence(start, frequency, today);

            // Assert
            // This assertion proves the "365 days = 1 Calendar Year" logic
            Assert.Equal(new DateTime(2025, 1, 1), result);
        }

        [Fact]
        public void GetNextOccurrence_LeapYear_PreservesFeb29()
        {
            // Arrange
            var start = new DateTime(2020, 2, 29); // Leap day
            var frequency = TimeSpan.FromDays(365); // "Yearly"
            var today = new DateTime(2021, 3, 1);

            // Act
            var result = DateCalculationService.GetNextOccurrence(start, frequency, today);

            // Assert
            // 2020-02-29 -> 2021-02-28 -> 2022-02-28
            Assert.Equal(new DateTime(2022, 2, 28), result);
        }

        [Fact]
        public void GetNextOccurrence_LeapYear_ReturnsToFeb29_OnNextLeapYear()
        {
            // Arrange
            var start = new DateTime(2020, 2, 29);
            var frequency = TimeSpan.FromDays(365);
            var today = new DateTime(2023, 3, 1); // After Feb 2023

            // Act
            var result = DateCalculationService.GetNextOccurrence(start, frequency, today);

            // Assert
            // 2020 -> 2021 (Feb 28) -> 2022 (Feb 28) -> 2023 (Feb 28).
            // Next is 2024 (Leap Year). Should match Feb 29.
            Assert.Equal(new DateTime(2024, 2, 29), result);
        }

        [Fact]
        public void GetNextOccurrence_FarPastDate_CatchesUp()
        {
            // Arrange
            var start = new DateTime(2000, 1, 1);
            var frequency = TimeSpan.FromDays(365);
            var today = new DateTime(2023, 6, 1);

            // Act
            var result = DateCalculationService.GetNextOccurrence(start, frequency, today);

            // Assert
            // Should be next Jan 1 after June 2023
            Assert.Equal(new DateTime(2024, 1, 1), result);
        }

        [Fact]
        public void GetNextOccurrence_ZeroFrequency_ReturnsOriginalDate()
        {
            // Arrange
            var start = new DateTime(2023, 1, 1);
            var frequency = TimeSpan.Zero;
            var today = new DateTime(2023, 6, 1);

            // Act
            var result = DateCalculationService.GetNextOccurrence(start, frequency, today);

            // Assert
            // Even though it's in the past, zero frequency means no recurrence.
            Assert.Equal(start, result);
        }

        [Fact]
        public void GetNextOccurrence_DueToday_ReturnsToday()
        {
            // Arrange
            var start = new DateTime(2023, 1, 1);
            var frequency = TimeSpan.FromDays(1);
            var today = new DateTime(2023, 1, 5);

            // Act
            var result = DateCalculationService.GetNextOccurrence(start, frequency, today);

            // Assert
            Assert.Equal(today, result);
        }

        [Fact]
        public void GetNextOccurrence_StandardInterval_DriftsCorrectly()
        {
            // Arrange
            var start = new DateTime(2023, 1, 1);
            var frequency = TimeSpan.FromDays(30);
            var today = new DateTime(2023, 2, 1); // 31 days later

            // Act
            var result = DateCalculationService.GetNextOccurrence(start, frequency, today);

            // Assert
            // Jan 1 + 30 days = Jan 31.
            // Jan 31 is not > today (Feb 1). Loop continues.
            // Jan 31 + 30 days = Mar 2 (2023 non-leap: Jan 31 + 28 = Feb 28 + 2 = Mar 2).
            Assert.Equal(new DateTime(2023, 3, 2), result);
        }

        [Fact]
        public void GetNextOccurrence_FutureDate_ReturnsOriginal()
        {
            // Arrange
            var start = new DateTime(2025, 1, 1);
            var frequency = TimeSpan.FromDays(1);
            var today = new DateTime(2023, 1, 1);

            // Act
            var result = DateCalculationService.GetNextOccurrence(start, frequency, today);

            // Assert
            Assert.Equal(start, result);
        }

        [Fact]
        public void GetNextOccurrence_MultipleOf365_ButNotOneYear_CalculatesCorrectly()
        {
            // Arrange
            var start = new DateTime(2020, 1, 1);
            var frequency = TimeSpan.FromDays(730); // 2 years
            var today = new DateTime(2021, 1, 1);

            // Act
            var result = DateCalculationService.GetNextOccurrence(start, frequency, today);

            // Assert
            // 2020 + 2 years = 2022.
            Assert.Equal(new DateTime(2022, 1, 1), result);
        }

        [Fact]
        public void GetNextOccurrence_LeapYearInterval_DriftsOneDayPerYear()
        {
             // Arrange
            var start = new DateTime(2021, 1, 1);
            var frequency = TimeSpan.FromDays(366); // Leap year length
            var today = new DateTime(2023, 1, 1);

            // Act
            var result = DateCalculationService.GetNextOccurrence(start, frequency, today);

            // Assert
            // 2021-01-01 + 366 days = 2022-01-02.
            // 2022-01-02 + 366 days = 2023-01-03.
            Assert.Equal(new DateTime(2023, 1, 3), result);
        }
    }
}
