using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Web.Controllers
{
    public class DebugOperationsController : Controller
    {
        private readonly IRepository _repository;

        public DebugOperationsController(IRepository repository)
        {
            _repository = repository;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedTestData()
        {
            var existingContacts = await _repository.CountAsync<Contact>();
            if (existingContacts == 0)
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
            }

            var existingTypes = await _repository.CountAsync<RelationshipType>();
            if (existingTypes == 0)
            {
                var types = new List<RelationshipType>
                {
                    new RelationshipType { Name = "Friend", OppositeName = "Friend", EntityType = EntityTypes.Person },
                    new RelationshipType { Name = "Spouse", OppositeName = "Spouse", EntityType = EntityTypes.Person },
                    new RelationshipType { Name = "Parent", OppositeName = "Child", EntityType = EntityTypes.Person },
                    new RelationshipType { Name = "Sibling", OppositeName = "Sibling", EntityType = EntityTypes.Person },
                    new RelationshipType { Name = "Colleague", OppositeName = "Colleague", EntityType = EntityTypes.Person }
                };
                await _repository.AddRangeAsync(types);
            }

            await _repository.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
