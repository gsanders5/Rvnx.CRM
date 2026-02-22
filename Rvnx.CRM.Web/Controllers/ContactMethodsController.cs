using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
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
            return entityId == Guid.Empty
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
                await Repository.AddAsync(contactInfo);
                await Repository.SaveChangesAsync();
                return RedirectToEntity(contactInfo.ContactId ?? Guid.Empty, EntityTypes.Person);
            }
            return View(contactInfoInput);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            ContactMethod? contactInfo = await Repository.GetByIdAsync<ContactMethod>(id.Value);

            if (contactInfo == null) return NotFound();

            ContactMethodFormDto dto = new()
            {
                Id = contactInfo.Id,
                Type = contactInfo.Type,
                Value = contactInfo.Value,
                Label = contactInfo.Label,
                EntityId = contactInfo.ContactId ?? Guid.Empty,
                EntityType = EntityTypes.Person
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
                    ContactMethod? existingContactInfo = await Repository.GetByIdAsync<ContactMethod>(id);
                    if (existingContactInfo == null) return NotFound();

                    existingContactInfo.UpdateEntity(contactInfoInput);

                    await Repository.UpdateAsync(existingContactInfo);
                    await Repository.SaveChangesAsync();

                    return RedirectToEntity(existingContactInfo.ContactId ?? Guid.Empty, EntityTypes.Person);
                }
                catch (Exception)
                {
                    if (!await Repository.ExistsAsync<ContactMethod>(contactInfoInput.Id.Value)) return NotFound();
                    else throw;
                }
            }

            return View(contactInfoInput);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();
            ContactMethod? contactInfo = await Repository.GetByIdAsync<ContactMethod>(id.Value);
            return contactInfo == null ? NotFound() : View(contactInfo);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            ContactMethod? contactInfo = await Repository.GetByIdAsync<ContactMethod>(id);
            if (contactInfo != null)
            {
                Guid entityId = contactInfo.ContactId ?? Guid.Empty;
                string entityType = EntityTypes.Person;
                await Repository.DeleteAsync<ContactMethod>(id);
                await Repository.SaveChangesAsync();
                return RedirectToEntity(entityId, entityType);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}