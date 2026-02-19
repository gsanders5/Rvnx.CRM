using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class FactsController : RepositoryController
    {
        public FactsController(IRepository repository) : base(repository)
        {
        }

        public IActionResult Create(Guid entityId, string entityType)
        {
            return entityId == Guid.Empty || string.IsNullOrEmpty(entityType)
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
                return RedirectToEntity(fact.EntityId, fact.EntityType);
            }
            return View(factDto);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            Fact? fact = await _repository.GetByIdAsync<Fact>(id.Value);
            
            if (fact == null) return NotFound();

            var dto = new FactFormDto
            {
                Id = fact.Id,
                Category = fact.Category,
                Value = fact.Value,
                EntityId = fact.EntityId,
                EntityType = fact.EntityType
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

                    return RedirectToEntity(existingFact.EntityId, existingFact.EntityType);
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
                Guid entityId = fact.EntityId;
                string entityType = fact.EntityType;
                await _repository.DeleteAsync<Fact>(id);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(entityId, entityType);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}