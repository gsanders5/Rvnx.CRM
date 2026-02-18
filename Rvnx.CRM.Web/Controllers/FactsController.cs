using Microsoft.AspNetCore.Mvc;
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
                : View(new Fact { EntityId = entityId, EntityType = entityType });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EntityId,EntityType,Category,Value")] Fact fact)
        {
            if (ModelState.IsValid)
            {
                fact.Id = Guid.NewGuid();
                await _repository.AddAsync(fact);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(fact.EntityId, fact.EntityType);
            }
            return View(fact);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            Fact? fact = await _repository.GetByIdAsync<Fact>(id.Value);
            return fact == null ? NotFound() : View(fact);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Category,Value")] Fact factInput)
        {
            if (id != factInput.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Fetch existing entity to preserve audit fields and prevent tampering
                    Fact? existingFact = await _repository.GetByIdAsync<Fact>(id);
                    if (existingFact == null) return NotFound();

                    // Only update user-editable fields
                    existingFact.Category = factInput.Category;
                    existingFact.Value = factInput.Value;
                    // EntityId, EntityType, CreatedDate, CreatedBy are preserved from existing entity

                    await _repository.UpdateAsync(existingFact);
                    await _repository.SaveChangesAsync();

                    return RedirectToEntity(existingFact.EntityId, existingFact.EntityType);
                }
                catch (Exception)
                {
                    if (!await _repository.ExistsAsync<Fact>(factInput.Id)) return NotFound();
                    else throw;
                }
            }

            // Re-fetch to get EntityId/EntityType for view
            Fact? fact = await _repository.GetByIdAsync<Fact>(id);
            if (fact != null)
            {
                // Merge input values back for display
                fact.Category = factInput.Category;
                fact.Value = factInput.Value;
                return View(fact);
            }

            return View(factInput);
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