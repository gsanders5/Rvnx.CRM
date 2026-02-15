using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
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
            // Get people from the database with pagination for performance
            var people = await _repository.ListAsync<Person>(skip: 0, take: 100);

            // Log the count for debugging
            _logger.LogInformation($"Found {people.Count} people in the database");

            return View(people);
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

        // Action to add some test data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedTestData()
        {
            var existingCount = await _repository.CountAsync<Contact>();
            if (existingCount == 0)
            {
                var testContacts = new List<Contact>
                {
                    new Contact { FirstName = "John", LastName = "Doe" },
                    new Contact { FirstName = "Jane", LastName = "Smith" },
                    new Contact { FirstName = "Michael", LastName = "Brown" },
                    new Contact { FirstName = "Emily", LastName = "Davis" },
                    new Contact { FirstName = "Robert", LastName = "Wilson" }
                };

                await _repository.AddRangeAsync(testContacts);
                await _repository.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}