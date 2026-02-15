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
            var model = new DashboardViewModel();

            // 1. Fetch Contacts
            var contacts = await _repository.ListAsync<Contact>();
            var contactDict = contacts.ToDictionary(c => c.Id, c => c);

            // 2. Fetch Upcoming Reminders
            var reminders = await _repository.ListAsync<Reminder>(r => !r.IsCompleted && r.DueDate >= DateTime.Today);

            // Map Reminders
            foreach (var reminder in reminders)
            {
                string entityName = "Unknown";
                if (reminder.EntityId != Guid.Empty && reminder.EntityType == EntityTypes.Person)
                {
                    if (contactDict.TryGetValue(reminder.EntityId, out var contact))
                    {
                        entityName = contact.FullName;
                    }
                }

                model.UpcomingEvents.Add(new UpcomingEventViewModel
                {
                    Title = reminder.Title,
                    Description = reminder.Description ?? "",
                    Date = reminder.DueDate,
                    Type = "Reminder",
                    RelatedEntityId = reminder.EntityId,
                    RelatedEntityName = entityName,
                    TimeUntil = GetTimeUntil(reminder.DueDate)
                });
            }

            // 3. Fetch Important Dates (Birthdays, Anniversaries, etc.)
            var importantDates = await _repository.ListAsync<SignificantDate>(d => d.EntityType == EntityTypes.Person);
            var today = DateTime.Today;

            foreach (var date in importantDates)
            {
                if (contactDict.TryGetValue(date.EntityId, out var contact))
                {
                    var originalDate = date.Date;
                    DateTime nextOccurrence = originalDate.Month == 2 && originalDate.Day == 29 && !DateTime.IsLeapYear(today.Year)
                        ? new DateTime(today.Year, 2, 28)
                        : new DateTime(today.Year, originalDate.Month, originalDate.Day);

                    // Handle leap year dates (Feb 29) on non-leap years

                    if (nextOccurrence < today)
                    {
                        // Move to next year
                        nextOccurrence = originalDate.Month == 2 && originalDate.Day == 29 && !DateTime.IsLeapYear(today.Year + 1)
                            ? new DateTime(today.Year + 1, 2, 28)
                            : new DateTime(today.Year + 1, originalDate.Month, originalDate.Day);
                    }

                    string desc = date.Title?.Equals("Birthday", StringComparison.OrdinalIgnoreCase) == true
                        ? $"Turns {nextOccurrence.Year - originalDate.Year}"
                        : $"{date.Title} ({originalDate.ToShortDateString()})";
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

            // Sort and limit events
            model.UpcomingEvents = model.UpcomingEvents.OrderBy(e => e.Date).Take(5).ToList();

            // 4. Graph Data
            // Nodes
            foreach (var contact in contacts)
            {
                model.GraphNodes.Add(new GraphNode
                {
                    Id = contact.Id.ToString(),
                    Name = contact.FullName,
                    Group = 1 // Contacts
                });
            }

            // Links
            var relationships = await _repository.ListAsync<Relationship>();
            foreach (var rel in relationships)
            {
                if (rel.EntityType == EntityTypes.Person)
                {
                    model.GraphLinks.Add(new GraphLink
                    {
                        Source = rel.EntityId.ToString(),
                        Target = rel.RelatedEntityId.ToString(),
                        Type = "Relationship"
                    });
                }
            }

            return View(model);
        }

        private string GetTimeUntil(DateTime date)
        {
            var span = date.Date - DateTime.Today;
            if (span.Days == 0) return "Today";
            if (span.Days == 1) return "Tomorrow";
            if (span.Days < 0) return "Overdue";
            return span.Days < 7 ? $"In {span.Days} days" : span.Days < 14 ? "In 1 week" : $"In {span.Days / 7} weeks";
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
