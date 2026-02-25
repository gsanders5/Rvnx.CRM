using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using Rvnx.CRM.Infrastructure.Services;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class DebugOperationsController(IRepository repository, IHostEnvironment environment) : AuthorizedController
    {
        private readonly IRepository _repository = repository;
        private readonly IHostEnvironment _environment = environment;

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
                if (contact.Id == Guid.Empty)
                {
                    contact.Id = Guid.NewGuid();
                }

                List<Address>? addresses = contact.Addresses?.ToList();
                List<ContactMethod>? infos = contact.ContactMethods?.ToList();
                List<SignificantDate>? dates = contact.SignificantDates?.ToList();

                contact.Addresses = [];
                contact.ContactMethods = [];
                contact.SignificantDates = [];

                await _repository.AddAsync(contact);
                await _repository.SaveChangesAsync();

                if (addresses != null)
                {
                    foreach (Address? addr in addresses)
                    {
                        addr.ContactId = contact.Id;
                        await _repository.AddAsync(addr);
                    }
                }

                if (infos != null)
                {
                    foreach (ContactMethod? info in infos)
                    {
                        info.ContactId = contact.Id;
                        await _repository.AddAsync(info);
                    }
                }

                if (dates != null)
                {
                    foreach (SignificantDate? date in dates)
                    {
                        date.ContactId = contact.Id;
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
            List<Contact> contacts = await _repository.ListAsync<Contact>();
            await _repository.DeleteRangeAsync(contacts);

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
            // 1. Get available Relationship Types (Static)
            List<RelationshipTypeDefinition> types = (List<RelationshipTypeDefinition>)RelationshipTypeService.GetAll();
            if (types.Count == 0)
            {
                TempData["Message"] = "No relationship types defined.";
                return RedirectToAction("Index");
            }

            // 2. Get Contacts
            List<Contact> contacts = await _repository.ListAsNoTrackingAsync<Contact>();
            if (contacts.Count < 2)
            {
                TempData["Message"] = "Not enough contacts to create relationships.";
                return RedirectToAction("Index");
            }

            // 3. Generate Random Relationships
            Random random = new();
            int relationshipsToCreate = Math.Min(contacts.Count * 2, 50);
            int createdCount = 0;

            for (int i = 0; i < relationshipsToCreate; i++)
            {
                Contact c1 = contacts[random.Next(contacts.Count)];
                Contact c2 = contacts[random.Next(contacts.Count)];

                if (c1.Id == c2.Id)
                {
                    continue;
                }

                RelationshipTypeDefinition type = types[random.Next(types.Count)];

                List<Relationship> existing = await _repository.ListAsNoTrackingAsync<Relationship>(r =>
                    r.EntityId == c1.Id && r.RelatedEntityId == c2.Id && r.RelationshipTypeId == type.Id);

                if (existing.Count > 0)
                {
                    continue;
                }

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
