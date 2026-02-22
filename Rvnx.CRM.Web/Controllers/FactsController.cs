using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class FactsController(IRepository repository) : RepositoryController(repository)
    {
        public IActionResult Create(Guid entityId, string entityType)
        {
            return entityId == Guid.Empty
                ? NotFound()
                : View(new FactFormDto { EntityId = entityId, EntityType = entityType });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FactFormDto factDto)
        {
            if (ModelState.IsValid)
            {
                Fact fact = factDto.ToEntity();
                await _repository.AddAsync(fact);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(fact.ContactId ?? Guid.Empty, EntityTypes.Person);
            }
            return View(factDto);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            Fact? fact = await _repository.GetByIdAsync<Fact>(id.Value);

            if (fact == null) return NotFound();

            FactFormDto dto = new()
            {
                Id = fact.Id,
                Category = fact.Category,
                Value = fact.Value,
                EntityId = fact.ContactId ?? Guid.Empty,
                EntityType = EntityTypes.Person
            };

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, FactFormDto factDto)
        {
            if (id != factDto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Fetch existing entity to preserve audit fields and prevent tampering
                    Fact? existingFact = await _repository.GetByIdAsync<Fact>(id);
                    if (existingFact == null) return NotFound();

                    existingFact.UpdateEntity(factDto);

                    await _repository.UpdateAsync(existingFact);
                    await _repository.SaveChangesAsync();

                    return RedirectToEntity(existingFact.ContactId ?? Guid.Empty, EntityTypes.Person);
                }
                catch (Exception)
                {
                    if (!await _repository.ExistsAsync<Fact>(factDto.Id.Value)) return NotFound();
                    else throw;
                }
            }

            return View(factDto);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();
            Fact? fact = await _repository.GetByIdAsync<Fact>(id.Value);
            return fact == null ? NotFound() : View(fact);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            Fact? fact = await _repository.GetByIdAsync<Fact>(id);
            if (fact != null)
            {
                Guid entityId = fact.ContactId ?? Guid.Empty;
                string entityType = EntityTypes.Person;
                await _repository.DeleteAsync<Fact>(id);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(entityId, entityType);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}