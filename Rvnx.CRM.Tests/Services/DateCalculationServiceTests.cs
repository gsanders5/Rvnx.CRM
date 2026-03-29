using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;

namespace Rvnx.CRM.Tests.Services;

public class DateCalculationServiceTests
{
    [Fact]
    public void GetNextOccurrenceWhenEventDateIsInFutureReturnsEventDate()
    {
        SignificantDate significantDate = new()
        {
            EventDate = new DateOnly(2025, 5, 15),
            RecurrenceType = RecurrenceType.Annual
        };
        DateOnly today = new(2023, 2, 1);

        DateOnly result = DateCalculationService.GetNextOccurrence(significantDate, today);

        Assert.Equal(new DateOnly(2025, 5, 15), result);
    }

    [Fact]
    public void GetNextOccurrenceAnnualSameYearWhenDateNotYetPassed()
    {
        SignificantDate significantDate = new()
        {
            EventDate = new DateOnly(2020, 5, 15),
            RecurrenceType = RecurrenceType.Annual
        };
        DateOnly today = new(2023, 2, 1);

        DateOnly result = DateCalculationService.GetNextOccurrence(significantDate, today);

        Assert.Equal(new DateOnly(2023, 5, 15), result);
    }

    [Fact]
    public void GetNextOccurrenceAnnualRollsToNextYearWhenDateHasPassed()
    {
        SignificantDate significantDate = new()
        {
            EventDate = new DateOnly(2020, 5, 15),
            RecurrenceType = RecurrenceType.Annual
        };
        DateOnly today = new(2023, 6, 1);

        DateOnly result = DateCalculationService.GetNextOccurrence(significantDate, today);

        Assert.Equal(new DateOnly(2024, 5, 15), result);
    }

    [Fact]
    public void GetNextOccurrenceAnnualFeb29ReturnsFeb28InNonLeapYear()
    {
        SignificantDate significantDate = new()
        {
            EventDate = new DateOnly(2020, 2, 29),
            RecurrenceType = RecurrenceType.Annual
        };
        DateOnly today = new(2023, 1, 1); // 2023 is not a leap year

        DateOnly result = DateCalculationService.GetNextOccurrence(significantDate, today);

        Assert.Equal(new DateOnly(2023, 2, 28), result);
    }

    [Fact]
    public void GetNextOccurrenceAnnualFeb29ReturnsFeb29InLeapYear()
    {
        SignificantDate significantDate = new()
        {
            EventDate = new DateOnly(2020, 2, 29),
            RecurrenceType = RecurrenceType.Annual
        };
        DateOnly today = new(2023, 3, 1); // After Feb 2023. Next is 2024 (Leap year)

        DateOnly result = DateCalculationService.GetNextOccurrence(significantDate, today);

        Assert.Equal(new DateOnly(2024, 2, 29), result);
    }

    [Fact]
    public void GetNextOccurrenceNoneAlwaysReturnsFixedEventDate()
    {
        SignificantDate significantDate = new()
        {
            EventDate = new DateOnly(2020, 5, 15),
            RecurrenceType = RecurrenceType.None
        };
        DateOnly today = new(2023, 6, 1);

        DateOnly result = DateCalculationService.GetNextOccurrence(significantDate, today);

        Assert.Equal(new DateOnly(2020, 5, 15), result);
    }

    [Fact]
    public void GetNextOccurrenceMonthlyAdvancesToNextMonthWhenDayHasPassed()
    {
        SignificantDate significantDate = new()
        {
            EventDate = new DateOnly(2023, 1, 15),
            RecurrenceType = RecurrenceType.Monthly
        };
        DateOnly today = new(2023, 3, 20);

        DateOnly result = DateCalculationService.GetNextOccurrence(significantDate, today);

        Assert.Equal(new DateOnly(2023, 4, 15), result);
    }

    [Fact]
    public void GetNextOccurrenceMonthlyClampsToEndOfMonth()
    {
        SignificantDate significantDate = new()
        {
            EventDate = new DateOnly(2023, 1, 31),
            RecurrenceType = RecurrenceType.Monthly
        };
        DateOnly today = new(2023, 2, 1);

        DateOnly result = DateCalculationService.GetNextOccurrence(significantDate, today);

        // Jan 31 advanced 1 month clamps to Feb 28 in 2023.
        Assert.Equal(new DateOnly(2023, 2, 28), result);
    }

    [Fact]
    public void GetNextOccurrenceCustomLandsOnValidIntervalBoundary()
    {
        SignificantDate significantDate = new()
        {
            EventDate = new DateOnly(2023, 1, 1),
            RecurrenceType = RecurrenceType.Custom,
            CustomIntervalDays = 10
        };
        DateOnly today = new(2023, 1, 15);

        DateOnly result = DateCalculationService.GetNextOccurrence(significantDate, today);

        // Start: Jan 1
        // Inter: +10 days -> Jan 11 (Passed)
        // Inter: +10 days -> Jan 21
        Assert.Equal(new DateOnly(2023, 1, 21), result);
    }

    [Fact]
    public void GetNextOccurrenceCustomWithNullIntervalReturnsEventDate()
    {
        SignificantDate significantDate = new()
        {
            EventDate = new DateOnly(2023, 1, 1),
            RecurrenceType = RecurrenceType.Custom,
            CustomIntervalDays = null
        };
        DateOnly today = new(2023, 1, 15);

        DateOnly result = DateCalculationService.GetNextOccurrence(significantDate, today);

        Assert.Equal(new DateOnly(2023, 1, 1), result);
    }

    [Fact]
    public void GetNextOccurrenceCustomWithZeroIntervalReturnsEventDate()
    {
        SignificantDate significantDate = new()
        {
            EventDate = new DateOnly(2023, 1, 1),
            RecurrenceType = RecurrenceType.Custom,
            CustomIntervalDays = 0
        };
        DateOnly today = new(2023, 1, 15);

        DateOnly result = DateCalculationService.GetNextOccurrence(significantDate, today);

        Assert.Equal(new DateOnly(2023, 1, 1), result);
    }

    [Fact]
    public void GetNextOccurrenceUnknownTypeReturnsEventDate()
    {
        SignificantDate significantDate = new()
        {
            EventDate = new DateOnly(2023, 1, 1),
            RecurrenceType = (RecurrenceType)999
        };
        DateOnly today = new(2023, 1, 15);

        DateOnly result = DateCalculationService.GetNextOccurrence(significantDate, today);

        Assert.Equal(new DateOnly(2023, 1, 1), result);
    }

    [Fact]
    public void GetNextOccurrenceAnnualOccurrenceIsTodayReturnsToday()
    {
        SignificantDate significantDate = new()
        {
            EventDate = new DateOnly(2020, 5, 15),
            RecurrenceType = RecurrenceType.Annual
        };
        DateOnly today = new(2023, 5, 15);

        DateOnly result = DateCalculationService.GetNextOccurrence(significantDate, today);

        Assert.Equal(new DateOnly(2023, 5, 15), result);
    }

    [Fact]
    public void GetNextOccurrenceMonthlyOccurrenceIsTodayReturnsToday()
    {
        SignificantDate significantDate = new()
        {
            EventDate = new DateOnly(2023, 1, 15),
            RecurrenceType = RecurrenceType.Monthly
        };
        DateOnly today = new(2023, 4, 15);

        DateOnly result = DateCalculationService.GetNextOccurrence(significantDate, today);

        Assert.Equal(new DateOnly(2023, 4, 15), result);
    }

    [Fact]
    public void GetNextOccurrenceCustomOccurrenceIsTodayReturnsToday()
    {
        SignificantDate significantDate = new()
        {
            EventDate = new DateOnly(2023, 1, 1),
            RecurrenceType = RecurrenceType.Custom,
            CustomIntervalDays = 10
        };
        DateOnly today = new(2023, 1, 21); // 1st + 20 days

        DateOnly result = DateCalculationService.GetNextOccurrence(significantDate, today);

        Assert.Equal(new DateOnly(2023, 1, 21), result);
    }

    [Fact]
    public void GetScheduledForDateReturnsNextOccurrenceMinusDaysBeforeEvent()
    {
        SignificantDate significantDate = new()
        {
            EventDate = new DateOnly(2023, 5, 15),
            RecurrenceType = RecurrenceType.Annual
        };
        ReminderOffset offset = new()
        {
            DaysBeforeEvent = 7
        };
        DateOnly today = new(2023, 5, 1);

        // Next occurrence is May 15. Scheduled for should be May 8.
        DateOnly result = DateCalculationService.GetScheduledForDate(significantDate, offset, today);

        Assert.Equal(new DateOnly(2023, 5, 8), result);
    }
}