using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;

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
            var contacts = FakeDataGenerator.GenerateContacts(10);

            foreach (var contact in contacts)
            {
                // Ensure ID is set
                if (contact.Id == Guid.Empty) contact.Id = Guid.NewGuid();

                // Detach related entities to add them separately (EF Core tracking issue prevention)
                var addresses = contact.Addresses?.ToList();
                var infos = contact.ContactInfos?.ToList();
                var dates = contact.ImportantDates?.ToList();

                contact.Addresses = [];
                contact.ContactInfos = [];
                contact.ImportantDates = [];

                await _repository.AddAsync(contact);
                await _repository.SaveChangesAsync(); // Save contact first to ensure it exists

                // Add related entities
                if (addresses != null)
                {
                    foreach (var addr in addresses)
                    {
                        addr.EntityId = contact.Id;
                        await _repository.AddAsync(addr);
                    }
                }

                if (infos != null)
                {
                    foreach (var info in infos)
                    {
                        info.EntityId = contact.Id;
                        await _repository.AddAsync(info);
                    }
                }

                if (dates != null)
                {
                    foreach (var date in dates)
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
            var contacts = await _repository.ListAsync<Contact>();
            await _repository.DeleteRangeAsync(contacts);

            // Also delete related generic entities... (simplified for now, assumes cascade or manual cleanup if implemented)
            // For a full reset, we might want to drop tables, but repository pattern might not expose that.
            // We will rely on deleting contacts and letting the user know they might have orphans if not careful,
            // or implement a full cleanup.

            // Clean up generic entities
            var notes = await _repository.ListAsync<Note>();
            await _repository.DeleteRangeAsync(notes);

            var reminders = await _repository.ListAsync<Reminder>();
            await _repository.DeleteRangeAsync(reminders);

            var dates = await _repository.ListAsync<ImportantDate>();
            await _repository.DeleteRangeAsync(dates);

            var infos = await _repository.ListAsync<ContactInfo>();
            await _repository.DeleteRangeAsync(infos);

            var facts = await _repository.ListAsync<Fact>();
            await _repository.DeleteRangeAsync(facts);

            var addresses = await _repository.ListAsync<Address>();
            await _repository.DeleteRangeAsync(addresses);

            var relationships = await _repository.ListAsync<Relationship>();
            await _repository.DeleteRangeAsync(relationships);

            await _repository.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRandomRelationships()
        {
            // 1. Ensure Relationship Types exist
            var types = await _repository.ListAsync<RelationshipType>();
            if (!types.Any())
            {
                var defaultTypes = new List<RelationshipType>
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
            var contacts = await _repository.ListAsync<Contact>();
            if (contacts.Count < 2)
            {
                TempData["Message"] = "Not enough contacts to create relationships.";
                return RedirectToAction("Index");
            }

            // 3. Generate Random Relationships
            var random = new Random();
            int relationshipsToCreate = Math.Min(contacts.Count * 2, 50); // Just a heuristic
            int createdCount = 0;

            for (int i = 0; i < relationshipsToCreate; i++)
            {
                var c1 = contacts[random.Next(contacts.Count)];
                var c2 = contacts[random.Next(contacts.Count)];

                if (c1.Id == c2.Id) continue;

                var type = types[random.Next(types.Count)];

                // Check if relationship already exists
                var existing = await _repository.ListAsync<Relationship>(r =>
                    r.EntityId == c1.Id && r.RelatedEntityId == c2.Id && r.RelationshipTypeId == type.Id);

                if (existing.Any()) continue;

                var rel = new Relationship
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
