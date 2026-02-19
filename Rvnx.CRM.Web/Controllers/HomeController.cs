using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Web.Controllers.Base;
using Rvnx.CRM.Web.Models;
using System.Diagnostics;

namespace Rvnx.CRM.Web.Controllers
{
    public class HomeController(ILogger<HomeController> logger, IRepository repository) : AuthorizedController
    {
        private readonly ILogger<HomeController> _logger = logger;
        private readonly IRepository _repository = repository;

        // Configuration constants
        private const int MaxUpcomingEvents = 5;
        private const int MaxEventsToProcess = 500;

        public async Task<IActionResult> Index()
        {
            DashboardViewModel model = new();

            // Optimization: Use ListAsNoTrackingAsync for read-only data to improve performance
            List<Contact> contacts = await _repository.ListAsNoTrackingAsync<Contact>(x => x.IsHidden == false);
            Dictionary<Guid, Contact> contactDict = contacts.ToDictionary(c => c.Id, c => c);

            // Use a PriorityQueue to efficiently track only the top N events
            // PriorityQueue in .NET 6+ is a min-heap, so events with smallest dates come first
            PriorityQueue<UpcomingEventViewModel, DateTime> topEvents = new();

            // Process reminders
            await ProcessRemindersAsync(topEvents, contactDict);

            // Process significant dates
            await ProcessSignificantDatesAsync(topEvents, contactDict);

            // Extract top events from priority queue (already sorted by date)
            while (topEvents.Count > 0 && model.UpcomingEvents.Count < MaxUpcomingEvents)
            {
                model.UpcomingEvents.Add(topEvents.Dequeue());
            }

            // Build graph data
            foreach (Contact contact in contacts)
            {
                model.GraphNodes.Add(new GraphNode
                {
                    Id = contact.Id.ToString(),
                    Name = contact.FullName,
                    Group = 1 // Contacts
                });
            }

            // Optimization: Filter relationships in the database query and use AsNoTracking
            List<Relationship> relationships = await _repository.ListAsNoTrackingAsync<Relationship>(r => r.EntityType == EntityTypes.Person);
            foreach (Relationship rel in relationships)
            {
                model.GraphLinks.Add(new GraphLink
                {
                    Source = rel.EntityId.ToString(),
                    Target = rel.RelatedEntityId.ToString(),
                    Type = "Relationship"
                });
            }

            return View(model);
        }

        private async Task ProcessRemindersAsync(
            PriorityQueue<UpcomingEventViewModel, DateTime> topEvents,
            Dictionary<Guid, Contact> contactDict)
        {
            // Fetch all reminders - SQLite/EFCore doesn't support TimeSpan comparison in queries
            List<Reminder> allReminders = await _repository.ListAsNoTrackingAsync<Reminder>();
            int processedCount = 0;
            foreach (Reminder reminder in allReminders)
            {
                if (reminder.IsCompleted && reminder.EventFrequency <= TimeSpan.Zero)
                    continue;

                DateTime nextDate = reminder.GetNextOccurrence();
                if (reminder.IsCompleted && nextDate == reminder.DueDate)
                    continue;

                string entityName = "Unknown";
                if (reminder.EntityId != Guid.Empty && reminder.EntityType == EntityTypes.Person)
                {
                    if (contactDict.TryGetValue(reminder.EntityId, out Contact? contact))
                    {
                        entityName = contact.FullName;
                    }
                }

                UpcomingEventViewModel eventVm = new()
                {
                    Title = reminder.Title,
                    Description = reminder.Description ?? "",
                    Date = nextDate,
                    Type = "Reminder",
                    RelatedEntityId = reminder.EntityId,
                    RelatedEntityName = entityName,
                    TimeUntil = GetTimeUntil(nextDate)
                };

                topEvents.Enqueue(eventVm, nextDate);
                processedCount++;
                if (processedCount >= MaxEventsToProcess)
                {
                    _logger.LogWarning("Reminder processing limit reached ({Limit}). Some reminders may not appear in dashboard.", MaxEventsToProcess);
                    break;
                }
            }
        }

        private async Task ProcessSignificantDatesAsync(
            PriorityQueue<UpcomingEventViewModel, DateTime> topEvents,
            Dictionary<Guid, Contact> contactDict)
        {
            List<SignificantDate> importantDates = await _repository.ListAsNoTrackingAsync<SignificantDate>(
                d => d.EntityType == EntityTypes.Person);

            int processedCount = 0;
            foreach (SignificantDate date in importantDates)
            {
                if (!contactDict.TryGetValue(date.EntityId, out Contact? contact))
                    continue;

                DateTime nextOccurrence = date.GetNextOccurrence();

                bool isBirthday = date.Title?.Equals("Birthday", StringComparison.OrdinalIgnoreCase) == true;
                string desc = isBirthday
                    ? $"Turns {nextOccurrence.Year - date.Date.Year}"
                    : $"{date.Title} ({date.Date.ToShortDateString()})";

                UpcomingEventViewModel eventVm = new()
                {
                    Title = $"{contact.FirstName}'s {date.Title}",
                    Description = desc,
                    Date = nextOccurrence,
                    Type = isBirthday ? "Birthday" : "Event",
                    RelatedEntityId = contact.Id,
                    RelatedEntityName = contact.FullName,
                    TimeUntil = GetTimeUntil(nextOccurrence)
                };

                topEvents.Enqueue(eventVm, nextOccurrence);

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

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}