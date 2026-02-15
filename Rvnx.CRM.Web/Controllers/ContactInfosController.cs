using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Web.Controllers
{
    public class ContactInfosController : Controller
    {
        private readonly IRepository _repository;

        public ContactInfosController(IRepository repository)
        {
            _repository = repository;
        }

        public IActionResult Create(Guid entityId, string entityType)
        {
            return entityId == Guid.Empty || string.IsNullOrEmpty(entityType)
                ? NotFound()
                : View(new ContactInfo { EntityId = entityId, EntityType = entityType });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EntityId,EntityType,Type,Value,Label")] ContactInfo contactInfo)
        {
            if (ModelState.IsValid)
            {
                contactInfo.Id = Guid.NewGuid();
                await _repository.AddAsync(contactInfo);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(contactInfo.EntityId, contactInfo.EntityType);
            }
            return View(contactInfo);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            var contactInfo = await _repository.GetByIdAsync<ContactInfo>(id.Value);
            return contactInfo == null ? NotFound() : View(contactInfo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,EntityId,EntityType,Type,Value,Label")] ContactInfo contactInfo)
        {
            if (id != contactInfo.Id) return NotFound();

            if (ModelState.IsValid)
            {
                await _repository.UpdateAsync(contactInfo);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(contactInfo.EntityId, contactInfo.EntityType);
            }
            return View(contactInfo);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();
            var contactInfo = await _repository.GetByIdAsync<ContactInfo>(id.Value);
            return contactInfo == null ? NotFound() : View(contactInfo);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var contactInfo = await _repository.GetByIdAsync<ContactInfo>(id);
            if (contactInfo != null)
            {
                var entityId = contactInfo.EntityId;
                var entityType = contactInfo.EntityType;
                await _repository.DeleteAsync<ContactInfo>(id);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(entityId, entityType);
            }
            return RedirectToAction("Index", "Home");
        }

        private IActionResult RedirectToEntity(Guid id, string type)
        {
            return type == EntityTypes.Person ? RedirectToAction("Details", "Contacts", new { id }) : RedirectToAction("Index", "Home");
        }
    }
}
