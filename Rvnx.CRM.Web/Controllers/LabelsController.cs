using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers;

public class LabelsController(ILabelService labelService) : AuthorizedController
{
    private readonly ILabelService _labelService = labelService;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        List<LabelDto> labels = await _labelService.GetAllAsync();
        return View(labels);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new LabelFormDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create(LabelFormDto formDto)
    {
        if (ModelState.IsValid)
        {
            LabelOperationResult result = await _labelService.CreateAsync(formDto.Name, formDto.Color);
            if (result.Success)
            {
                return RedirectToAction(nameof(Index));
            }

            foreach (string error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        return View(formDto);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        List<LabelDto> labels = await _labelService.GetAllAsync();
        LabelDto? label = labels.FirstOrDefault(l => l.Id == id);
        return label == null
            ? NotFound()
            : View(new LabelFormDto { Id = label.Id, Name = label.Name, Color = label.Color });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, LabelFormDto formDto)
    {
        if (id != formDto.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            LabelOperationResult result = await _labelService.UpdateAsync(id, formDto.Name, formDto.Color);
            if (result.Success)
            {
                return RedirectToAction(nameof(Index));
            }

            if (result.IsNotFound)
            {
                return NotFound();
            }

            foreach (string error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        return View(formDto);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _labelService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
