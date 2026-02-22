using Microsoft.Extensions.Logging;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Dashboard;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Core.Services;

public class DashboardService(IRepository repository, ILogger<DashboardService> logger) : IDashboardService
{
    private readonly IRepository _repository = repository;
    private readonly ILogger<DashboardService> _logger = logger;

    private const int MaxUpcomingEvents = 5;
    private const int MaxEventsToProcess = 500;

    public async Task<DashboardDto> GetDashboardDataAsync()
    {
        DashboardDto result = new();

        List<Contact> contacts = await _repository.ListAsNoTrackingAsync<Contact>(x => x.IsHidden == false);
        Dictionary<Guid, Contact> contactDict = contacts.ToDictionary(c => c.Id, c => c);

        PriorityQueue<UpcomingEventDto, DateTime> topEvents = new();

        await ProcessRemindersAsync(topEvents, contactDict);
        await ProcessSignificantDatesAsync(topEvents, contactDict);

        while (topEvents.Count > 0 && result.UpcomingEvents.Count < MaxUpcomingEvents)
        {
            result.UpcomingEvents.Add(topEvents.Dequeue());
        }

        foreach (Contact contact in contacts)
        {
            result.GraphNodes.Add(new GraphNodeDto
            {
                Id = contact.Id.ToString(),
                Name = contact.FullName,
                Group = 1
            });
        }

        List<Relationship> relationships = await _repository.ListAsNoTrackingAsync<Relationship>(r => r.EntityType == EntityTypes.Person);
        foreach (Relationship rel in relationships)
        {
            result.GraphLinks.Add(new GraphLinkDto
            {
                Source = rel.EntityId.ToString(),
                Target = rel.RelatedEntityId.ToString(),
                Type = "Relationship"
            });
        }

        return result;
    }

    private async Task ProcessRemindersAsync(
        PriorityQueue<UpcomingEventDto, DateTime> topEvents,
        Dictionary<Guid, Contact> contactDict)
    {
        // Optimization: Filter out completed non-recurring reminders at DB level to reduce memory usage and processing time.
        List<Reminder> allReminders = await _repository.ListAsNoTrackingAsync<Reminder>(
            r => !r.IsCompleted || r.EventFrequency > TimeSpan.Zero);

        int processedCount = 0;
        foreach (Reminder reminder in allReminders)
        {
            DateTime nextDate = reminder.GetNextOccurrence();
            if (reminder.IsCompleted && nextDate == reminder.DueDate)
                continue;

            string entityName = "Unknown";
            if (reminder.ContactId.HasValue)
            {
                if (contactDict.TryGetValue(reminder.ContactId.Value, out Contact? contact))
                {
                    entityName = contact.FullName;
                }
            }

            UpcomingEventDto eventDto = new()
            {
                Title = reminder.Title,
                Description = reminder.Description ?? "",
                Date = nextDate,
                Type = "Reminder",
                RelatedEntityId = reminder.ContactId ?? Guid.Empty,
                RelatedEntityName = entityName,
                TimeUntil = GetTimeUntil(nextDate)
            };

            topEvents.Enqueue(eventDto, nextDate);
            processedCount++;
            if (processedCount >= MaxEventsToProcess)
            {
                _logger.LogWarning("Reminder processing limit reached ({Limit}). Some reminders may not appear in dashboard.", MaxEventsToProcess);
                break;
            }
        }
    }

    private async Task ProcessSignificantDatesAsync(
        PriorityQueue<UpcomingEventDto, DateTime> topEvents,
        Dictionary<Guid, Contact> contactDict)
    {
        List<SignificantDate> importantDates = await _repository.ListAsNoTrackingAsync<SignificantDate>(
            d => d.ContactId != null);

        int processedCount = 0;
        foreach (SignificantDate date in importantDates)
        {
            if (!contactDict.TryGetValue(date.ContactId ?? Guid.Empty, out Contact? contact))
                continue;

            DateTime nextOccurrence = date.GetNextOccurrence();

            bool isBirthday = date.Title?.Equals(SignificantDateTitles.Birthday, StringComparison.OrdinalIgnoreCase) == true;
            string desc = isBirthday
                ? $"Turns {nextOccurrence.Year - date.Date.Year}"
                : $"{date.Title} ({date.Date.ToShortDateString()})";

            UpcomingEventDto eventDto = new()
            {
                Title = $"{contact.FirstName}'s {date.Title}",
                Description = desc,
                Date = nextOccurrence,
                Type = isBirthday ? SignificantDateTitles.Birthday : "Event",
                RelatedEntityId = contact.Id,
                RelatedEntityName = contact.FullName,
                TimeUntil = GetTimeUntil(nextOccurrence)
            };

            topEvents.Enqueue(eventDto, nextOccurrence);

            processedCount++;
            if (processedCount >= MaxEventsToProcess)
            {
                _logger.LogWarning("Significant date processing limit reached ({Limit}). Some dates may not appear in dashboard.", MaxEventsToProcess);
                break;
            }
        }
    }

    private static string GetTimeUntil(DateTime date)
    {
        TimeSpan span = date.Date - DateTime.Today;
        return span.Days switch
        {
            0 => "Today",
            1 => "Tomorrow",
            < 0 => "Overdue",
            < 7 => $"In {span.Days} days",
            < 14 => "In 1 week",
            _ => $"In {span.Days / 7} weeks"
        };
    }
}
