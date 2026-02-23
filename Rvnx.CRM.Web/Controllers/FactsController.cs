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
        public async Task<IActionResult> Create(Guid entityId, string entityType)
        {
            return entityId == Guid.Empty || await IsPartialContactAsync(entityId)
                ? NotFound()
                : View(new FactFormDto { EntityId = entityId, EntityType = entityType });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FactFormDto factDto)
        {
            if (await IsPartialContactAsync(factDto.EntityId)) return NotFound();

            if (ModelState.IsValid)
            {
                Fact fact = factDto.ToEntity();
                await Repository.AddAsync(fact);
                await Repository.SaveChangesAsync();
                return RedirectToEntity(fact.ContactId ?? Guid.Empty, EntityTypes.Person);
            }
            return View(factDto);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Fact? fact = await Repository.GetByIdAsync<Fact>(id.Value);

            if (fact == null || await IsPartialContactAsync(fact.ContactId ?? Guid.Empty))
            {
                return NotFound();
            }

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
            if (id != factDto.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Fetch existing entity to preserve audit fields and prevent tampering
                    Fact? existingFact = await Repository.GetByIdAsync<Fact>(id);
                    if (existingFact == null || await IsPartialContactAsync(existingFact.ContactId ?? Guid.Empty))
                    {
                        return NotFound();
                    }

                    existingFact.UpdateEntity(factDto);

                    await Repository.UpdateAsync(existingFact);
                    await Repository.SaveChangesAsync();

                    return RedirectToEntity(existingFact.ContactId ?? Guid.Empty, EntityTypes.Person);
                }
                catch (Exception)
                {
                    if (!await Repository.ExistsAsync<Fact>(factDto.Id.Value))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(factDto);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Fact? fact = await Repository.GetByIdAsync<Fact>(id.Value);
            return fact == null || await IsPartialContactAsync(fact.ContactId ?? Guid.Empty) ? NotFound() : View(fact);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            Fact? fact = await Repository.GetByIdAsync<Fact>(id);
            if (fact != null)
            {
                Guid entityId = fact.ContactId ?? Guid.Empty;
                string entityType = EntityTypes.Person;
                await Repository.DeleteAsync<Fact>(id);
                await Repository.SaveChangesAsync();
                return RedirectToEntity(entityId, entityType);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}