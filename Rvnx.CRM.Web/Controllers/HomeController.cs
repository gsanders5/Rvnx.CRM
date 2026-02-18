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
    public class HomeController : AuthorizedController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IRepository _repository;

        public HomeController(ILogger<HomeController> logger, IRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        public async Task<IActionResult> Index()
        {
            DashboardViewModel model = new();

            // Optimization: Use ListAsNoTrackingAsync for read-only data to improve performance
            List<Contact> contacts = await _repository.ListAsNoTrackingAsync<Contact>();
            Dictionary<Guid, Contact> contactDict = contacts.ToDictionary(c => c.Id, c => c);

            // Fetch all reminders and filter in memory because SQLite/EFCore doesn't support TimeSpan comparison > TimeSpan.Zero
            // Using AsNoTracking since we only read
            List<Reminder> allReminders = await _repository.ListAsNoTrackingAsync<Reminder>();
            List<Reminder> reminders = allReminders.Where(r => !r.IsCompleted || r.EventFrequency > TimeSpan.Zero).ToList();

            foreach (Reminder reminder in reminders)
            {
                DateTime nextDate = reminder.GetNextOccurrence();

                if (reminder.IsCompleted && nextDate == reminder.DueDate) continue;

                string entityName = "Unknown";
                if (reminder.EntityId != Guid.Empty && reminder.EntityType == EntityTypes.Person)
                {
                    if (contactDict.TryGetValue(reminder.EntityId, out Contact? contact))
                    {
                        entityName = contact.FullName;
                    }
                }

                model.UpcomingEvents.Add(new UpcomingEventViewModel
                {
                    Title = reminder.Title,
                    Description = reminder.Description ?? "",
                    Date = nextDate,
                    Type = "Reminder",
                    RelatedEntityId = reminder.EntityId,
                    RelatedEntityName = entityName,
                    TimeUntil = GetTimeUntil(nextDate)
                });
            }

            // Use ListAsNoTrackingAsync
            List<SignificantDate> importantDates = await _repository.ListAsNoTrackingAsync<SignificantDate>(d => d.EntityType == EntityTypes.Person);

            foreach (SignificantDate date in importantDates)
            {
                if (contactDict.TryGetValue(date.EntityId, out Contact? contact))
                {
                    DateTime nextOccurrence = date.GetNextOccurrence();

                    string desc = date.Title?.Equals("Birthday", StringComparison.OrdinalIgnoreCase) == true
                        ? $"Turns {nextOccurrence.Year - date.Date.Year}"
                        : $"{date.Title} ({date.Date.ToShortDateString()})";

                    model.UpcomingEvents.Add(new UpcomingEventViewModel
                    {
                        Title = $"{contact.FirstName}'s {date.Title}",
                        Description = desc,
                        Date = nextOccurrence,
                        Type = date.Title?.Equals("Birthday", StringComparison.OrdinalIgnoreCase) == true ? "Birthday" : "Event",
                        RelatedEntityId = contact.Id,
                        RelatedEntityName = contact.FullName,
                        TimeUntil = GetTimeUntil(nextOccurrence)
                    });
                }
            }

            model.UpcomingEvents = model.UpcomingEvents.OrderBy(e => e.Date).Take(5).ToList();

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

        // Removed GetNextOccurrence helper methods as logic is now in Models

        private string GetTimeUntil(DateTime date)
        {
            TimeSpan span = date.Date - DateTime.Today;
            return span.Days == 0
                ? "Today"
                : span.Days == 1
                ? "Tomorrow"
                : span.Days < 0
                ? "Overdue"
                : span.Days < 7 ? $"In {span.Days} days" : span.Days < 14 ? "In 1 week" : $"In {span.Days / 7} weeks";
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
