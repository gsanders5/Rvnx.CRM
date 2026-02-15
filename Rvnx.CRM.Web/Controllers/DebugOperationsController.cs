using Microsoft.AspNetCore.Mvc;
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

                contact.Addresses = null;
                contact.ContactInfos = null;
                contact.ImportantDates = null;

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

            await _repository.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
