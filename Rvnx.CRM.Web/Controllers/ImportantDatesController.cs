using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class ImportantDatesController : BaseAuthorizedController
    {
        private readonly IRepository _repository;

        public ImportantDatesController(IRepository repository)
        {
            _repository = repository;
        }

        // GET: ImportantDates/Create
        public IActionResult Create(Guid entityId, string entityType)
        {
            return View(new ImportantDateDto
            {
                EntityId = entityId,
                EntityType = entityType,
                Date = DateTime.Today
            });
        }

        // POST: ImportantDates/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Date,EntityId,EntityType")] ImportantDateDto dto)
        {
            if (ModelState.IsValid)
            {
                // Enforce unique Birthday
                if (string.Equals(dto.Title, "Birthday", StringComparison.OrdinalIgnoreCase))
                {
                    var existingBirthday = (await _repository.ListAsync<ImportantDate>(d =>
                        d.EntityId == dto.EntityId &&
                        d.EntityType == dto.EntityType &&
                        d.Title == "Birthday")).Any();

                    if (existingBirthday)
                    {
                        ModelState.AddModelError("Title", "A birthday is already set for this contact.");
                        return View(dto);
                    }
                }

                var importantDate = new ImportantDate
                {
                    Id = Guid.NewGuid(),
                    Title = dto.Title,
                    Description = dto.Description,
                    Date = dto.Date,
                    EntityId = dto.EntityId,
                    EntityType = dto.EntityType
                };

                await _repository.AddAsync(importantDate);
                await _repository.SaveChangesAsync();

                // Redirect back to the entity details
                return RedirectToAction("Details", GetControllerForEntity(dto.EntityType), new { id = dto.EntityId });
            }
            return View(dto);
        }

        // GET: ImportantDates/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var importantDate = await _repository.GetByIdAsync<ImportantDate>(id.Value);
            return importantDate == null ? NotFound() : View(importantDate.ToDto());
        }

        // POST: ImportantDates/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Title,Description,Date,EntityId,EntityType")] ImportantDateDto dto)
        {
            if (id != dto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var importantDate = await _repository.GetByIdAsync<ImportantDate>(id);
                    if (importantDate == null) return NotFound();

                    // Enforce unique Birthday (if title changed to Birthday)
                    if (string.Equals(dto.Title, "Birthday", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(importantDate.Title, "Birthday", StringComparison.OrdinalIgnoreCase))
                    {
                        var existingBirthday = (await _repository.ListAsync<ImportantDate>(d =>
                            d.EntityId == dto.EntityId &&
                            d.EntityType == dto.EntityType &&
                            d.Title == "Birthday")).Any();

                        if (existingBirthday)
                        {
                            ModelState.AddModelError("Title", "A birthday is already set for this contact.");
                            return View(dto);
                        }
                    }

                    importantDate.Title = dto.Title;
                    importantDate.Description = dto.Description;
                    importantDate.Date = dto.Date;

                    await _repository.UpdateAsync(importantDate);
                    await _repository.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _repository.ExistsAsync<ImportantDate>(dto.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction("Details", GetControllerForEntity(dto.EntityType), new { id = dto.EntityId });
            }
            return View(dto);
        }

        // GET: ImportantDates/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var importantDate = await _repository.GetByIdAsync<ImportantDate>(id.Value);
            return importantDate == null ? NotFound() : View(importantDate.ToDto());
        }

        // POST: ImportantDates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var importantDate = await _repository.GetByIdAsync<ImportantDate>(id);
            if (importantDate != null)
            {
                var entityId = importantDate.EntityId;
                var entityType = importantDate.EntityType;

                await _repository.DeleteAsync<ImportantDate>(id);
                await _repository.SaveChangesAsync();

                return RedirectToAction("Details", GetControllerForEntity(entityType), new { id = entityId });
            }
            return RedirectToAction("Index", "Home"); // Fallback
        }

        private string GetControllerForEntity(string entityType)
        {
            return entityType switch
            {
                EntityTypes.Person => "Contacts",
                // Add other types as needed
                _ => "Home"
            };
        }
    }
}
