using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class SignificantDatesController(IRepository repository) : RepositoryController(repository)
    {
        public async Task<IActionResult> Create(Guid entityId, string entityType)
        {
            return !await IsValidContactAsync(entityId)
                ? NotFound()
                : View(new SignificantDateDto
                {
                    EntityId = entityId,
                    EntityType = entityType,
                    Date = DateTime.Today,
                    EventFrequency = TimeSpan.FromDays(365) // Default to Yearly
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SignificantDateDto dto)
        {
            if (!await IsValidContactAsync(dto.EntityId))
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (string.Equals(dto.Title, SignificantDateTitles.Birthday, StringComparison.OrdinalIgnoreCase))
                {
                    if (await IsBirthdayAlreadySetAsync(dto.EntityId))
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
                    ContactId = dto.EntityId,
                    RemindMe = dto.RemindMe,
                    EventFrequency = dto.EventFrequency
                };

                await Repository.AddAsync(importantDate);
                await Repository.SaveChangesAsync();

                return RedirectToEntity(dto.EntityId, dto.EntityType);
            }

            return View(dto);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            SignificantDate? importantDate = await Repository.GetByIdAsync<SignificantDate>(id.Value);
            return importantDate == null || !await IsValidContactAsync(importantDate.ContactId ?? Guid.Empty)
                ? NotFound()
                : View(importantDate.ToDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, SignificantDateDto dto)
        {
            if (id != dto.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    SignificantDate? importantDate = await Repository.GetByIdAsync<SignificantDate>(id);
                    if (importantDate == null || !await IsValidContactAsync(importantDate.ContactId ?? Guid.Empty))
                    {
                        return NotFound();
                    }

                    if (string.Equals(dto.Title, SignificantDateTitles.Birthday, StringComparison.OrdinalIgnoreCase))
                    {
                        if (await IsBirthdayAlreadySetAsync(dto.EntityId, dto.Id))
                        {
                            ModelState.AddModelError("Title", "A birthday is already set for this contact.");
                            return View(dto);
                        }

                        dto.EventFrequency = TimeSpan.FromDays(365);
                    }

                    importantDate.Title = dto.Title;
                    importantDate.Description = dto.Description;
                    importantDate.Date = dto.Date;
                    importantDate.RemindMe = dto.RemindMe;
                    importantDate.EventFrequency = dto.EventFrequency;

                    await Repository.UpdateAsync(importantDate);
                    await Repository.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await Repository.ExistsAsync<SignificantDate>(dto.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToEntity(dto.EntityId, dto.EntityType);
            }

            return View(dto);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            SignificantDate? importantDate = await Repository.GetByIdAsync<SignificantDate>(id.Value);
            return importantDate == null || !await IsValidContactAsync(importantDate.ContactId ?? Guid.Empty)
                ? NotFound()
                : View(importantDate.ToDto());
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            SignificantDate? importantDate = await Repository.GetByIdAsync<SignificantDate>(id);
            if (importantDate != null)
            {
                Guid entityId = importantDate.ContactId ?? Guid.Empty;
                string entityType = EntityTypes.Person;

                await Repository.DeleteAsync<SignificantDate>(id);
                await Repository.SaveChangesAsync();

                return RedirectToEntity(entityId, entityType);
            }

            return RedirectToAction("Index", "Home");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons", Justification = "EF Core cannot translate string.Equals with StringComparison. .ToLower() is used for SQLite-compatible translatable case-insensitivity.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "EF Core cannot translate .ToLower(CultureInfo).")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1311:Specify a culture or use an invariant version", Justification = "EF Core cannot translate .ToLower(CultureInfo) or .ToLowerInvariant().")]
        private async Task<bool> IsBirthdayAlreadySetAsync(Guid contactId, Guid? excludeId = null)
        {
            return (await Repository.CountAsync<SignificantDate>(d =>
                       d.ContactId == contactId &&
                       d.Id != (excludeId ?? Guid.Empty) &&
                       d.Title != null &&
                       d.Title.ToLower() == SignificantDateTitles.Birthday.ToLower())) >
                   0;
        }
    }
}
