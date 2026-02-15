using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class FactsController : AuthorizedController
    {
        private readonly IRepository _repository;

        public FactsController(IRepository repository)
        {
            _repository = repository;
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
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,EntityId,EntityType,Category,Value")] Fact fact)
        {
            if (id != fact.Id) return NotFound();

            if (ModelState.IsValid)
            {
                await _repository.UpdateAsync(fact);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(fact.EntityId, fact.EntityType);
            }
            return View(fact);
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

        private IActionResult RedirectToEntity(Guid id, string type)
        {
            return type == EntityTypes.Person ? RedirectToAction("Details", "Contacts", new { id }) : RedirectToAction("Index", "Home");
        }
    }
}
