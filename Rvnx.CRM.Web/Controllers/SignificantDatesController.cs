using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class SignificantDatesController(IRepository repository) : RepositoryController(repository)
    {
        public IActionResult Create(Guid entityId, string entityType)
        {
            return View(new SignificantDateDto
            {
                EntityId = entityId,
                EntityType = entityType,
                Date = DateTime.Today,
                EventFrequency = TimeSpan.FromDays(365) // Default to Yearly
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Date,EntityId,EntityType,RemindMe,EventFrequency")] SignificantDateDto dto)
        {
            if (ModelState.IsValid)
            {
                if (string.Equals(dto.Title, SignificantDateTitles.Birthday, StringComparison.OrdinalIgnoreCase))
                {
                    bool existingBirthday = (await _repository.CountAsync<SignificantDate>(d =>
                        d.EntityId == dto.EntityId &&
                        d.EntityType == dto.EntityType &&
                        string.Equals(d.Title, SignificantDateTitles.Birthday, StringComparison.OrdinalIgnoreCase))) > 0;

                    if (existingBirthday)
                    {
                        ModelState.AddModelError("Title", "A birthday is already set for this contact.");
                        return View(dto);
                    }

                    dto.EventFrequency = TimeSpan.FromDays(365);
                }

                SignificantDate importantDate = new()
                {
                    Id = Guid.NewGuid(),
                    Title = dto.Title,
                    Description = dto.Description,
                    Date = dto.Date,
                    EntityId = dto.EntityId,
                    EntityType = dto.EntityType,
                    RemindMe = dto.RemindMe,
                    EventFrequency = dto.EventFrequency
                };

                await _repository.AddAsync(importantDate);
                await _repository.SaveChangesAsync();

                return RedirectToEntity(dto.EntityId, dto.EntityType);
            }
            return View(dto);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            SignificantDate? importantDate = await _repository.GetByIdAsync<SignificantDate>(id.Value);
            return importantDate == null ? NotFound() : View(importantDate.ToDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Title,Description,Date,EntityId,EntityType,RemindMe,EventFrequency")] SignificantDateDto dto)
        {
            if (id != dto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    SignificantDate? importantDate = await _repository.GetByIdAsync<SignificantDate>(id);
                    if (importantDate == null) return NotFound();

                    if (string.Equals(dto.Title, SignificantDateTitles.Birthday, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.Equals(importantDate.Title, SignificantDateTitles.Birthday, StringComparison.OrdinalIgnoreCase))
                        {
                            bool existingBirthday = (await _repository.CountAsync<SignificantDate>(d =>
                                d.EntityId == dto.EntityId &&
                                d.EntityType == dto.EntityType &&
                                string.Equals(d.Title, SignificantDateTitles.Birthday, StringComparison.OrdinalIgnoreCase))) > 0;

                            if (existingBirthday)
                            {
                                ModelState.AddModelError("Title", "A birthday is already set for this contact.");
                                return View(dto);
                            }
                        }
                        dto.EventFrequency = TimeSpan.FromDays(365);
                    }

                    importantDate.Title = dto.Title;
                    importantDate.Description = dto.Description;
                    importantDate.Date = dto.Date;
                    importantDate.RemindMe = dto.RemindMe;
                    importantDate.EventFrequency = dto.EventFrequency;

                    await _repository.UpdateAsync(importantDate);
                    await _repository.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _repository.ExistsAsync<SignificantDate>(dto.Id)) return NotFound();
                    else throw;
                }
                return RedirectToEntity(dto.EntityId, dto.EntityType);
            }
            return View(dto);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            SignificantDate? importantDate = await _repository.GetByIdAsync<SignificantDate>(id.Value);
            return importantDate == null ? NotFound() : View(importantDate.ToDto());
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            SignificantDate? importantDate = await _repository.GetByIdAsync<SignificantDate>(id);
            if (importantDate != null)
            {
                Guid entityId = importantDate.EntityId;
                string entityType = importantDate.EntityType;

                await _repository.DeleteAsync<SignificantDate>(id);
                await _repository.SaveChangesAsync();

                return RedirectToEntity(entityId, entityType);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
