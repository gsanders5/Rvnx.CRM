using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class DebugOperationsController : AuthorizedController
    {
        private readonly IRepository _repository;
        private readonly IHostEnvironment _environment;

        public DebugOperationsController(IRepository repository, IHostEnvironment environment)
        {
            _repository = repository;
            _environment = environment;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!_environment.IsDevelopment())
            {
                context.Result = new NotFoundResult();
                return;
            }
            base.OnActionExecuting(context);
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedTestData()
        {
            List<Contact> contacts = FakeDataGenerator.GenerateContacts(10);

            foreach (Contact contact in contacts)
            {
                // Ensure ID is set
                if (contact.Id == Guid.Empty) contact.Id = Guid.NewGuid();

                // Detach related entities to add them separately (EF Core tracking issue prevention)
                List<Address>? addresses = contact.Addresses?.ToList();
                List<ContactMethod>? infos = contact.ContactMethods?.ToList();
                List<SignificantDate>? dates = contact.SignificantDates?.ToList();

                contact.Addresses = [];
                contact.ContactMethods = [];
                contact.SignificantDates = [];

                await _repository.AddAsync(contact);
                await _repository.SaveChangesAsync(); // Save contact first to ensure it exists

                // Add related entities
                if (addresses != null)
                {
                    foreach (Address? addr in addresses)
                    {
                        addr.EntityId = contact.Id;
                        await _repository.AddAsync(addr);
                    }
                }

                if (infos != null)
                {
                    foreach (ContactMethod? info in infos)
                    {
                        info.EntityId = contact.Id;
                        await _repository.AddAsync(info);
                    }
                }

                if (dates != null)
                {
                    foreach (SignificantDate? date in dates)
                    {
                        date.EntityId = contact.Id;
                        await _repository.AddAsync(date);
                    }
                }
            }

            await _repository.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetDatabase()
        {
            // This is dangerous in prod, but fine for debug controller
            List<Contact> contacts = await _repository.ListAsync<Contact>();
            await _repository.DeleteRangeAsync(contacts);

            // Also delete related generic entities... (simplified for now, assumes cascade or manual cleanup if implemented)
            // For a full reset, we might want to drop tables, but repository pattern might not expose that.
            // We will rely on deleting contacts and letting the user know they might have orphans if not careful,
            // or implement a full cleanup.

            // Clean up generic entities
            List<Note> notes = await _repository.ListAsync<Note>();
            await _repository.DeleteRangeAsync(notes);

            List<Reminder> reminders = await _repository.ListAsync<Reminder>();
            await _repository.DeleteRangeAsync(reminders);

            List<SignificantDate> dates = await _repository.ListAsync<SignificantDate>();
            await _repository.DeleteRangeAsync(dates);

            List<ContactMethod> infos = await _repository.ListAsync<ContactMethod>();
            await _repository.DeleteRangeAsync(infos);

            List<Fact> facts = await _repository.ListAsync<Fact>();
            await _repository.DeleteRangeAsync(facts);

            List<Address> addresses = await _repository.ListAsync<Address>();
            await _repository.DeleteRangeAsync(addresses);

            List<Relationship> relationships = await _repository.ListAsync<Relationship>();
            await _repository.DeleteRangeAsync(relationships);

            await _repository.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRandomRelationships()
        {
            // 1. Ensure Relationship Types exist
            List<RelationshipType> types = await _repository.ListAsync<RelationshipType>();
            if (!types.Any())
            {
                List<RelationshipType> defaultTypes = new()
                {
                    new() { Id = Guid.NewGuid(), Name = "Friend", OppositeName = "Friend", EntityType = EntityTypes.Person },
                    new() { Id = Guid.NewGuid(), Name = "Colleague", OppositeName = "Colleague", EntityType = EntityTypes.Person },
                    new() { Id = Guid.NewGuid(), Name = "Family", OppositeName = "Family", EntityType = EntityTypes.Person },
                    new() { Id = Guid.NewGuid(), Name = "Parent", OppositeName = "Child", EntityType = EntityTypes.Person },
                    new() { Id = Guid.NewGuid(), Name = "Manager", OppositeName = "Direct Report", EntityType = EntityTypes.Person }
                };
                await _repository.AddRangeAsync(defaultTypes);
                await _repository.SaveChangesAsync();
                types = defaultTypes;
            }

            // 2. Get Contacts
            List<Contact> contacts = await _repository.ListAsync<Contact>();
            if (contacts.Count < 2)
            {
                TempData["Message"] = "Not enough contacts to create relationships.";
                return RedirectToAction("Index");
            }

            // 3. Generate Random Relationships
            Random random = new();
            int relationshipsToCreate = Math.Min(contacts.Count * 2, 50); // Just a heuristic
            int createdCount = 0;

            for (int i = 0; i < relationshipsToCreate; i++)
            {
                Contact c1 = contacts[random.Next(contacts.Count)];
                Contact c2 = contacts[random.Next(contacts.Count)];

                if (c1.Id == c2.Id) continue;

                RelationshipType type = types[random.Next(types.Count)];

                // Check if relationship already exists
                List<Relationship> existing = await _repository.ListAsync<Relationship>(r =>
                    r.EntityId == c1.Id && r.RelatedEntityId == c2.Id && r.RelationshipTypeId == type.Id);

                if (existing.Any()) continue;

                Relationship rel = new()
                {
                    Id = Guid.NewGuid(),
                    EntityId = c1.Id,
                    RelatedEntityId = c2.Id,
                    EntityType = EntityTypes.Person,
                    RelationshipTypeId = type.Id,
                    Description = "Randomly generated"
                };

                await _repository.AddAsync(rel);
                createdCount++;
            }

            await _repository.SaveChangesAsync();
            TempData["Message"] = $"Created {createdCount} relationships.";
            return RedirectToAction("Index");
        }
    }
}
