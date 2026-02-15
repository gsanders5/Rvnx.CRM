using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Web.Models;
using System.Diagnostics;

namespace Rvnx.CRM.Web.Controllers
{
    public class HomeController : Controller
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
                if (reminder.EntityId.HasValue && reminder.EntityType == EntityTypes.Person)
                {
                    if (contactDict.TryGetValue(reminder.EntityId.Value, out var contact))
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

            // 3. Fetch Birthdays (from Contacts)
            var today = DateTime.Today;
            foreach (var contact in contacts)
            {
                if (contact.Birthday.HasValue)
                {
                    var bday = contact.Birthday.Value;
                    DateTime nextBday;

                    // Handle leap year birthdays (Feb 29) on non-leap years
                    if (bday.Month == 2 && bday.Day == 29 && !DateTime.IsLeapYear(today.Year))
                    {
                        nextBday = new DateTime(today.Year, 2, 28);
                    }
                    else
                    {
                        nextBday = new DateTime(today.Year, bday.Month, bday.Day);
                    }

                    if (nextBday < today)
                    {
                        nextBday = nextBday.AddYears(1);
                    }

                    model.UpcomingEvents.Add(new UpcomingEventViewModel
                    {
                        Title = $"{contact.FirstName}'s Birthday",
                        Description = $"Turns {nextBday.Year - bday.Year}",
                        Date = nextBday,
                        Type = "Birthday",
                        RelatedEntityId = contact.Id,
                        RelatedEntityName = contact.FullName,
                        TimeUntil = GetTimeUntil(nextBday)
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
            if (span.Days < 7) return $"In {span.Days} days";
            if (span.Days < 14) return "In 1 week";
            return $"In {span.Days / 7} weeks";
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
