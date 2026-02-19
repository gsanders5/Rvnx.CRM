using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class ContactMethodsController(IRepository repository) : RepositoryController(repository)
    {
        public IActionResult Create(Guid entityId, string entityType)
        {
            return entityId == Guid.Empty || string.IsNullOrEmpty(entityType)
                ? NotFound()
                : View(new ContactMethodFormDto { EntityId = entityId, EntityType = entityType });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContactMethodFormDto contactInfoInput)
        {
            if (ModelState.IsValid)
            {
                ContactMethod contactInfo = contactInfoInput.ToEntity();
                await _repository.AddAsync(contactInfo);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(contactInfo.EntityId, contactInfo.EntityType);
            }
            return View(contactInfoInput);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            ContactMethod? contactInfo = await _repository.GetByIdAsync<ContactMethod>(id.Value);

            if (contactInfo == null) return NotFound();

            ContactMethodFormDto dto = new()
            {
                Id = contactInfo.Id,
                Type = contactInfo.Type,
                Value = contactInfo.Value,
                Label = contactInfo.Label,
                EntityId = contactInfo.EntityId,
                EntityType = contactInfo.EntityType
            };

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, ContactMethodFormDto contactInfoInput)
        {
            if (id != contactInfoInput.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    ContactMethod? existingContactInfo = await _repository.GetByIdAsync<ContactMethod>(id);
                    if (existingContactInfo == null) return NotFound();

                    existingContactInfo.UpdateEntity(contactInfoInput);

                    await _repository.UpdateAsync(existingContactInfo);
                    await _repository.SaveChangesAsync();

                    return RedirectToEntity(existingContactInfo.EntityId, existingContactInfo.EntityType);
                }
                catch (Exception)
                {
                    if (!await _repository.ExistsAsync<ContactMethod>(contactInfoInput.Id.Value)) return NotFound();
                    else throw;
                }
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