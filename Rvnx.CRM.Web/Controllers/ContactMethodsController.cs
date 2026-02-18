using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class ContactMethodsController : RepositoryController
    {
        public ContactMethodsController(IRepository repository) : base(repository)
        {
        }

        public IActionResult Create(Guid entityId, string entityType)
        {
            return entityId == Guid.Empty || string.IsNullOrEmpty(entityType)
                ? NotFound()
                : View(new ContactMethod { EntityId = entityId, EntityType = entityType });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EntityId,EntityType,Type,Value,Label")] ContactMethod contactInfo)
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
            ContactMethod? contactInfo = await _repository.GetByIdAsync<ContactMethod>(id.Value);
            return contactInfo == null ? NotFound() : View(contactInfo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Type,Value,Label")] ContactMethod contactInfoInput)
        {
            if (id != contactInfoInput.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Fetch existing entity to preserve audit fields and prevent tampering
                    ContactMethod? existingContactInfo = await _repository.GetByIdAsync<ContactMethod>(id);
                    if (existingContactInfo == null) return NotFound();

                    // Only update user-editable fields
                    existingContactInfo.Type = contactInfoInput.Type;
                    existingContactInfo.Value = contactInfoInput.Value;
                    existingContactInfo.Label = contactInfoInput.Label;
                    // EntityId, EntityType, CreatedDate, CreatedBy are preserved from existing entity

                    await _repository.UpdateAsync(existingContactInfo);
                    await _repository.SaveChangesAsync();

                    return RedirectToEntity(existingContactInfo.EntityId, existingContactInfo.EntityType);
                }
                catch (Exception)
                {
                    if (!await _repository.ExistsAsync<ContactMethod>(contactInfoInput.Id)) return NotFound();
                    else throw;
                }
            }

            // Re-fetch to get EntityId/EntityType for view
            ContactMethod? contactInfo = await _repository.GetByIdAsync<ContactMethod>(id);
            if (contactInfo != null)
            {
                // Merge input values back for display
                contactInfo.Type = contactInfoInput.Type;
                contactInfo.Value = contactInfoInput.Value;
                contactInfo.Label = contactInfoInput.Label;
                return View(contactInfo);
            }

            return View(contactInfoInput);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();
            ContactMethod? contactInfo = await _repository.GetByIdAsync<ContactMethod>(id.Value);
            return contactInfo == null ? NotFound() : View(contactInfo);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            ContactMethod? contactInfo = await _repository.GetByIdAsync<ContactMethod>(id);
            if (contactInfo != null)
            {
                Guid entityId = contactInfo.EntityId;
                string entityType = contactInfo.EntityType;
                await _repository.DeleteAsync<ContactMethod>(id);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(entityId, entityType);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}