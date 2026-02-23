using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers;

public class LabelsController(ILabelService labelService) : AuthorizedController
{
    private readonly ILabelService _labelService = labelService;

    public async Task<IActionResult> Index()
    {
        var labels = await _labelService.GetAllAsync();
        return View(labels);
    }

    public IActionResult Create()
    {
        return View(new LabelFormDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LabelFormDto formDto)
    {
        if (ModelState.IsValid)
        {
            var result = await _labelService.CreateAsync(formDto.Name, formDto.Color);
            if (result.Success)
            {
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }
        return View(formDto);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var labels = await _labelService.GetAllAsync();
        var label = labels.FirstOrDefault(l => l.Id == id);
        if (label == null)
            return NotFound();

        return View(new LabelFormDto { Id = label.Id, Name = label.Name, Color = label.Color });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, LabelFormDto formDto)
    {
        if (id != formDto.Id)
            return NotFound();

        if (ModelState.IsValid)
        {
            var result = await _labelService.UpdateAsync(id, formDto.Name, formDto.Color);
            if (result.Success)
            {
                return RedirectToAction(nameof(Index));
            }
            if (result.IsNotFound)
            {
                return NotFound();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }
        return View(formDto);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _labelService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
