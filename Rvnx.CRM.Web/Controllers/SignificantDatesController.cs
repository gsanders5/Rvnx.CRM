using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers;

public class SignificantDatesController(ISignificantDateService significantDateService, IRepository repository)
    : RepositoryController(repository)
{
    private readonly ISignificantDateService _significantDateService = significantDateService;

    [HttpGet]
    public async Task<IActionResult> Index(Guid contactId)
    {
        if (!await IsValidContactAsync(contactId))
        {
            return NotFound();
        }

        List<SignificantDateDto> dates = await _significantDateService.GetByContactAsync(contactId);
        ViewBag.ContactId = contactId;
        return View(dates);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid contactId)
    {
        return !await IsValidContactAsync(contactId)
            ? NotFound()
            : View(new Rvnx.CRM.Core.DTOs.Dates.CreateSignificantDateRequest
            {
                ContactId = contactId,
                EventDate = DateOnly.FromDateTime(DateTime.Today),
                RecurrenceType = RecurrenceType.Annual,
                ReminderOffsetDays = [0, 7, 30]
            });
    }

    [HttpPost]
    public async Task<IActionResult> Create(Rvnx.CRM.Core.DTOs.Dates.CreateSignificantDateRequest dto)
    {
        if (ModelState.IsValid)
        {
            SignificantDateDto sdDto = new()
            {
                ContactId = dto.ContactId,
                Title = dto.Title,
                Description = dto.Description,
                EventDate = dto.EventDate,
                RecurrenceType = dto.RecurrenceType,
                CustomIntervalDays = dto.CustomIntervalDays,
                IsActive = true,
                ReminderOffsets = dto.ReminderOffsetDays
                    .Select(d => new ReminderOffsetDto { DaysBeforeEvent = d, IsActive = true }).ToList()
            };

            OperationResult result = await _significantDateService.CreateAsync(sdDto);
            if (result.Success)
            {
                return RedirectToAction(nameof(Index), new { contactId = dto.ContactId });
            }

            if (result.IsConflict)
            {
                ModelState.AddModelError("Title", result.ErrorMessage!);
                return View(dto);
            }

            if (result.IsNotFound)
            {
                return NotFound();
            }
        }

        return View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        SignificantDateDto? dto = await _significantDateService.GetDtoAsync(id.Value);
        if (dto == null)
        {
            return NotFound();
        }

        ViewBag.Offsets = dto.ReminderOffsets;

        return View(new Rvnx.CRM.Core.DTOs.Dates.UpdateSignificantDateRequest
        {
            Id = dto.Id,
            ContactId = dto.ContactId,
            Title = dto.Title,
            Description = dto.Description,
            EventDate = dto.EventDate,
            RecurrenceType = dto.RecurrenceType,
            CustomIntervalDays = dto.CustomIntervalDays,
            IsActive = dto.IsActive
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, Rvnx.CRM.Core.DTOs.Dates.UpdateSignificantDateRequest dto)
    {
        if (id != dto.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            SignificantDateDto sdDto = new()
            {
                Id = dto.Id,
                ContactId = dto.ContactId,
                Title = dto.Title,
                Description = dto.Description,
                EventDate = dto.EventDate,
                RecurrenceType = dto.RecurrenceType,
                CustomIntervalDays = dto.CustomIntervalDays,
                IsActive = dto.IsActive
            };

            OperationResult result = await _significantDateService.UpdateAsync(id, sdDto);
            if (result.Success)
            {
                return RedirectToAction(nameof(Index), new { contactId = dto.ContactId });
            }

            if (result.IsConflict)
            {
                ModelState.AddModelError("Title", result.ErrorMessage!);
                return View(dto);
            }

            if (result.IsNotFound)
            {
                return NotFound();
            }
        }

        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> AddOffset(Guid significantDateId, int daysBeforeEvent)
    {
        await _significantDateService.AddReminderOffsetAsync(significantDateId, daysBeforeEvent);
        return RedirectToAction(nameof(Edit), new { id = significantDateId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteOffset(Guid offsetId, Guid significantDateId)
    {
        await _significantDateService.DeleteReminderOffsetAsync(offsetId);
        return RedirectToAction(nameof(Edit), new { id = significantDateId });
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id, Guid contactId)
    {
        OperationResult result = await _significantDateService.DeleteAsync(id);
        return result.Success
            ? RedirectToAction(nameof(Index), new { contactId })
            : RedirectToAction("Index", "Home");
    }
}
