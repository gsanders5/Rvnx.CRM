using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class SignificantDatesController(ISignificantDateService significantDateService, IRepository repository) : RepositoryController(repository)
    {
        private readonly ISignificantDateService _significantDateService = significantDateService;

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
            if (ModelState.IsValid)
            {
                OperationResult result = await _significantDateService.CreateAsync(dto);
                if (result.Success)
                {
                    return RedirectToEntity(result.RedirectId, result.RedirectType);
                }

                if (result.ErrorMessage == "A birthday is already set for this contact.")
                {
                    ModelState.AddModelError("Title", result.ErrorMessage);
                    return View(dto);
                }

                if (result.ErrorMessage == "Contact not found.") return NotFound();
            }

            return View(dto);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            SignificantDateDto? dto = await _significantDateService.GetDtoAsync(id.Value);
            return dto == null ? NotFound() : View(dto);
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
                    OperationResult result = await _significantDateService.UpdateAsync(id, dto);
                    if (result.Success)
                    {
                        return RedirectToEntity(result.RedirectId, result.RedirectType);
                    }

                    if (result.ErrorMessage == "A birthday is already set for this contact.")
                    {
                        ModelState.AddModelError("Title", result.ErrorMessage);
                        return View(dto);
                    }

                    if (result.ErrorMessage == "Significant date not found.") return NotFound();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw;
                }
            }

            return View(dto);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            SignificantDateDto? dto = await _significantDateService.GetDtoAsync(id.Value);
            return dto == null ? NotFound() : View(dto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            OperationResult result = await _significantDateService.DeleteAsync(id);
            if (result.Success)
            {
                return RedirectToEntity(result.RedirectId, result.RedirectType);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
