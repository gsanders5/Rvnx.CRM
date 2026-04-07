using Microsoft.Extensions.Logging;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Dashboard;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Core.Services;

public class DashboardService(IRepository repository, ILogger<DashboardService> logger) : IDashboardService
{
    private readonly IRepository _repository = repository;
    private readonly ILogger<DashboardService> _logger = logger;

    private const int MaxUpcomingEvents = 5;
    private const int MaxEventsToProcess = 500;

    private static readonly Action<ILogger, int, Exception?> LogSignificantDateProcessingLimitReached =
        LoggerMessage.Define<int>(
            LogLevel.Warning,
            new EventId(2, nameof(LogSignificantDateProcessingLimitReached)),
            "Significant date processing limit reached ({Limit}). Some dates may not appear in dashboard.");

    internal sealed record ContactSummary(Guid Id, string FirstName, string? LastName, string? Gender, DateTime CreatedDate, DateTime LastChangedDate)
    {
        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    /// <inheritdoc />
    public async Task<DashboardDto> GetDashboardDataAsync()
    {
        DashboardDto dashboard = new();

        List<ContactSummary> contacts = await _repository.ListProjectedAsync<Contact, ContactSummary>(
            x => !x.IsHidden && !x.IsPartial,
            c => new ContactSummary(
                c.Id,
                c.FirstName,
                c.LastName,
                c.Gender,
                c.CreatedDate,
                c.LastChangedDate));

        Dictionary<Guid, ContactSummary> contactDict = contacts.ToDictionary(c => c.Id);

        List<Guid> contactIds = [.. contacts.Select(c => c.Id)];

        List<(Guid ContactId, Guid AttachmentId)> profileAttachments = contactIds.Count > 0
            ? await _repository.ListProjectedByChunkedContainsAsync<Attachment, (Guid, Guid), Guid>(
                contactIds,
                chunk => a =>
                    a.ContactId != null && a.AttachmentType == AttachmentTypes.ProfileImage &&
                    chunk.Contains(a.ContactId.Value),
                a => new ValueTuple<Guid, Guid>(a.ContactId!.Value, a.Id))
            : [];

        Dictionary<Guid, Guid> attachmentMap = new(profileAttachments.Count);
        foreach ((Guid contactId, Guid attachmentId) in profileAttachments)
        {
            attachmentMap.TryAdd(contactId, attachmentId);
        }

        PriorityQueue<UpcomingEventDto, DateTime> topEvents = new();
        await EnqueueUpcomingEventsAsync(topEvents, contactDict);

        while (topEvents.Count > 0 && dashboard.UpcomingEvents.Count < MaxUpcomingEvents)
        {
            dashboard.UpcomingEvents.Add(topEvents.Dequeue());
        }

        foreach (ContactSummary contact in contacts)
        {
            string? photoUrl = null;
            if (attachmentMap.TryGetValue(contact.Id, out Guid attachmentId))
            {
                photoUrl = $"/Attachments/Thumbnail/{attachmentId}?maxWidth=80&maxHeight=80";
            }

            dashboard.GraphNodes.Add(new GraphNodeDto
            {
                Id = contact.Id.ToString(),
                Name = contact.FullName,
                Group = 1,
                PhotoUrl = photoUrl,
                Gender = contact.Gender
            });
        }

        List<(Guid EntityId, Guid RelatedEntityId)> relationships =
            await _repository.ListProjectedAsync<Relationship, (Guid EntityId, Guid RelatedEntityId)>(
                r => r.EntityType == EntityTypes.Person,
                r => new ValueTuple<Guid, Guid>(r.EntityId, r.RelatedEntityId));

        foreach ((Guid EntityId, Guid RelatedEntityId) in relationships)
        {
            dashboard.GraphLinks.Add(new GraphLinkDto
            {
                Source = EntityId.ToString(),
                Target = RelatedEntityId.ToString(),
                Type = "Relationship"
            });
        }

        const int MaxRecentContacts = 5;
        DateTime sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        dashboard.RecentContacts = contacts
            .OrderByDescending(c => c.LastChangedDate)
            .Take(MaxRecentContacts)
            .Select(c => new RecentContactDto
            {
                Id = c.Id,
                FullName = c.FullName,
                CreatedDate = c.CreatedDate,
                LastChangedDate = c.LastChangedDate,
                ProfileImageId = attachmentMap.TryGetValue(c.Id, out Guid aid) ? aid : null,
                IsNew = c.CreatedDate >= sevenDaysAgo
            })
            .ToList();

        // Optimization: avoid multiple iterations and collection instantiations by iterating over the relationships collection once
        int contactsWithRelationshipsCount = 0;
        HashSet<Guid> uniqueContactsWithRelationships = new(relationships.Count * 2);
        foreach ((Guid entityId, Guid relatedEntityId) in relationships)
        foreach (var relationship in relationships)
        {
            if (uniqueContactsWithRelationships.Add(relationship.EntityId) && contactDict.ContainsKey(relationship.EntityId))
            {
                contactsWithRelationshipsCount++;
            }
            if (uniqueContactsWithRelationships.Add(relationship.RelatedEntityId) && contactDict.ContainsKey(relationship.RelatedEntityId))
            {
                contactsWithRelationshipsCount++;
            }
        }

        int birthdayCount = await _repository.CountAsync<SignificantDate>(sd =>
            sd.ContactId.HasValue &&
            sd.Contact != null &&
            !sd.Contact.IsHidden &&
            !sd.Contact.IsPartial &&
            sd.Title == SignificantDateTitles.Birthday);

        int hiddenContactsCount =
            await _repository.CountAsync<Contact>(x => x.IsHidden && !x.IsPartial);

        dashboard.Stats = new DashboardStatsDto
        {
            TotalContacts = contacts.Count,
            ContactsWithBirthday = birthdayCount,
            ContactsWithRelationships = contactsWithRelationshipsCount,
            ContactsHidden = hiddenContactsCount
        };

        return dashboard;
    }

    private async Task EnqueueUpcomingEventsAsync(
        PriorityQueue<UpcomingEventDto, DateTime> topEvents,
        Dictionary<Guid, ContactSummary> contactDict)
    {
        List<SignificantDate> importantDates =
            await _repository.ListAsNoTrackingAsync<SignificantDate>(d => d.ContactId != null && d.IsActive);

        int processedCount = 0;
        foreach (SignificantDate date in importantDates)
        {
            if (!contactDict.TryGetValue(date.ContactId ?? Guid.Empty, out ContactSummary? contact))
            {
                continue;
            }

            DateOnly nextOcc = date.GetNextOccurrence();
            DateTime nextOccurrence = nextOcc.ToDateTime(TimeOnly.MinValue);

            bool isBirthday = date.Title?.Equals(SignificantDateTitles.Birthday, StringComparison.OrdinalIgnoreCase) == true;
            string desc = isBirthday
                ? $"Turns {nextOcc.Year - date.EventDate.Year}"
                : $"{date.Title} ({date.EventDate.ToShortDateString()})";

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
                LogSignificantDateProcessingLimitReached(_logger, MaxEventsToProcess, null);
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