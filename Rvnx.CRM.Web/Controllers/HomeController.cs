using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
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
            // Get all people from the database
            var people = await _repository.ListAsync<Person>();

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
        public async Task<IActionResult> SeedTestData()
        {
            var existingCount = await _repository.CountAsync<Person>();

            if (existingCount == 0)
            {
                var testPeople = new List<Person>
                {
                    new Person
                    {
                        FirstName = "John",
                        LastName = "Doe",
                        Email = "john.doe@example.com",
                        PhoneNumber = "555-0101",
                    },
                    new Person
                    {
                        FirstName = "Jane",
                        LastName = "Smith",
                        Email = "jane.smith@example.com",
                        PhoneNumber = "555-0102",
                    },
                    new Person
                    {
                        FirstName = "Bob",
                        LastName = "Johnson",
                        Email = "bob.johnson@example.com",
                        PhoneNumber = "555-0103",
                    }
                };

                await _repository.AddRangeAsync(testPeople);
                await _repository.SaveChangesAsync();

                _logger.LogInformation($"Added {testPeople.Count} test people to the database");
            }

            return RedirectToAction("Index");
        }
    }
}