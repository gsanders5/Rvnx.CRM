using FileTypeChecker.Web.Attributes;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers.Base;
using Rvnx.CRM.Web.ViewModels.Contact;

namespace Rvnx.CRM.Web.Controllers;

public class ContactsController(
    ILogger<ContactsController> logger,
    ICurrentUserService currentUserService,
    IContactImportService contactImportService,
    IContactExportService contactExportService,
    ICsvExportService csvExportService,
    IContactManagementService contactManagementService,
    IContactReadService contactReadService,
    ISelfContactService selfContactService,
    IFileValidationService fileValidationService) : AuthorizedController
{
    private readonly ILogger<ContactsController> _logger = logger;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IContactImportService _contactImportService = contactImportService;
    private readonly IContactExportService _contactExportService = contactExportService;
    private readonly ICsvExportService _csvExportService = csvExportService;
    private readonly IContactManagementService _contactManagementService = contactManagementService;
    private readonly IContactReadService _contactReadService = contactReadService;
    private readonly ISelfContactService _selfContactService = selfContactService;
    private readonly IFileValidationService _fileValidationService = fileValidationService;

    private static readonly Action<ILogger, Exception?> LogErrorImportingVcf =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1, nameof(LogErrorImportingVcf)),
            "Error importing VCF");

    [HttpGet]
    public async Task<IActionResult> Self()
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return RedirectToAction("Index", "Home");
        }

        Guid? selfContactId = await _selfContactService.GetSelfContactIdAsync();

        return selfContactId.HasValue
            ? RedirectToAction(nameof(Details), new { id = selfContactId })
            : RedirectToAction(nameof(CreateSelf));
    }

    [HttpGet]
    public async Task<IActionResult> CreateSelf()
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return RedirectToAction("Index", "Home");
        }

        Guid? selfContactId = await _selfContactService.GetSelfContactIdAsync();

        if (selfContactId.HasValue)
        {
            return RedirectToAction(nameof(Details), new { id = selfContactId });
        }

        ContactFormDto? formDto = await _selfContactService.GetSelfContactFormAsync();
        if (formDto == null)
        {
            return RedirectToAction("Index", "Home");
        }

        ContactCreateViewModel viewModel = new()
        {
            FirstName = formDto.FirstName,
            LastName = formDto.LastName,
            Nickname = formDto.Nickname,
            Email = formDto.Email,
            Phone = formDto.Phone,
            JobTitle = formDto.JobTitle,
            Company = formDto.Company,
            Birthday = formDto.Birthday,
            RemindOnBirthday = formDto.RemindOnBirthday,
            IsHidden = formDto.IsHidden,
            Pronouns = formDto.Pronouns,
            Gender = formDto.Gender,
            Religion = formDto.Religion,
            IsSelfCreate = true,
            PronounOptions = PersonalAttributeOptions.Pronouns,
            GenderOptions = PersonalAttributeOptions.Gender
        };

        return View("Create", viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSelf(ContactCreateViewModel contactDto)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Unauthorized();
        }

        contactDto.IsHidden = false;
        NormalizeContactForm(contactDto);

        if (ModelState.IsValid)
        {
            ContactOperationResult result =
                await _selfContactService.CreateSelfContactAsync(contactDto);
            if (result.Success && result.ContactId.HasValue)
            {
                return RedirectToAction(nameof(Details), new { id = result.ContactId.Value });
            }

            foreach (string error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        contactDto.IsSelfCreate = true;
        contactDto.PronounOptions = PersonalAttributeOptions.Pronouns;
        contactDto.GenderOptions = PersonalAttributeOptions.Gender;
        return View("Create", contactDto);
    }

    [HttpGet]
    public async Task<IActionResult> Index(bool showHidden = false)
    {
        List<ContactDto> contactDtos = await _contactReadService.GetIndexDataAsync(showHidden);

        ContactIndexViewModel viewModel = new()
        {
            Contacts = contactDtos,
            SuccessMessage = TempData["SuccessMessage"] as string
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        ContactDetailDto? contactDto = await _contactReadService.GetContactDetailsAsync(id.Value);
        return contactDto == null ? NotFound() : View(contactDto);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ContactCreateViewModel
        {
            IsSelfCreate = false,
            PronounOptions = PersonalAttributeOptions.Pronouns,
            GenderOptions = PersonalAttributeOptions.Gender
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(ContactCreateViewModel contactDto)
    {
        NormalizeContactForm(contactDto);

        if (ModelState.IsValid)
        {
            ContactOperationResult result = await _contactManagementService.CreateContactAsync(contactDto);
            if (result.Success)
            {
                return RedirectToAction(nameof(Index));
            }

            foreach (string error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        contactDto.IsSelfCreate = false;
        contactDto.PronounOptions = PersonalAttributeOptions.Pronouns;
        contactDto.GenderOptions = PersonalAttributeOptions.Gender;
        return View(contactDto);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        ContactFormDto? dto = await _contactReadService.GetContactFormAsync(id.Value);
        if (dto == null)
        {
            return NotFound();
        }

        bool hasRelationships = await _contactReadService.HasRelationshipsAsync(id.Value);
        ContactEditViewModel viewModel = MapToEditViewModel(dto, dto.ProfileImageId, hasRelationships);

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, ContactFormDto contactDto, [AllowImages] IFormFile? profileImage)
    {
        if (id != contactDto.Id)
        {
            return NotFound();
        }

        if (!await _contactReadService.ContactExistsAsync(id))
        {
            return NotFound();
        }

        if (profileImage != null && !_fileValidationService.IsAllowedFileSize(profileImage.Length))
        {
            ModelState.AddModelError("profileImage", "File is too large.");
        }

        NormalizeContactForm(contactDto);

        if (ModelState.IsValid)
        {
            Stream? stream = null;
            if (profileImage != null && profileImage.Length > 0)
            {
                stream = profileImage.OpenReadStream();
            }

            using (stream)
            {
                ContactOperationResult result = await _contactManagementService.UpdateContactAsync(id, contactDto,
                    stream, profileImage?.FileName, profileImage?.ContentType);

                if (result.Success)
                {
                    return RedirectToAction(nameof(Details), new { id });
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
        }

        ContactFormDto? formConfig = await _contactReadService.GetContactFormAsync(id);
        if (formConfig != null)
        {
            contactDto.AllLabels = formConfig.AllLabels;
            contactDto.AssignedLabelIds = formConfig.AssignedLabelIds;
        }

        bool hasRelationships = await _contactReadService.HasRelationshipsAsync(id);
        ContactEditViewModel viewModel = MapToEditViewModel(contactDto, formConfig?.ProfileImageId, hasRelationships);

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        ContactDetailDto? contactDto = await _contactReadService.GetContactDetailsAsync(id.Value);
        return contactDto == null ? NotFound() : View(contactDto);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _contactManagementService.DeleteContactAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UnsetPhoto(Guid id)
    {
        if (!await _contactReadService.ContactExistsAsync(id))
        {
            return NotFound();
        }

        ContactOperationResult result = await _contactManagementService.UnsetProfilePhotoAsync(id);

        return result.Success
            ? RedirectToAction(nameof(Edit), new { id })
            : BadRequest("Could not unset profile photo.");
    }

    [HttpPost]
    public async Task<IActionResult> AssignLabel(Guid contactId, Guid labelId,
        [FromServices] ILabelService labelService, string? returnUrl = null)
    {
        if (contactId != Guid.Empty && labelId != Guid.Empty)
        {
            await labelService.AssignLabelAsync(contactId, labelId);
        }

        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Edit), new { id = contactId });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveLabel(Guid contactId, Guid labelId,
        [FromServices] ILabelService labelService, string? returnUrl = null)
    {
        if (contactId != Guid.Empty && labelId != Guid.Empty)
        {
            await labelService.RemoveLabelAsync(contactId, labelId);
        }

        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Edit), new { id = contactId });
    }

    [HttpGet]
    public IActionResult Import()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("file", "Please select a file.");
            return View();
        }

        if (!_fileValidationService.IsAllowedFileSize(file.Length))
        {
            ModelState.AddModelError("file", "File is too large.");
            return View();
        }

        // Note: File.TypeChecker does not support .vcf, so we rely on extension validation.
        if (!Path.GetExtension(file.FileName).Equals(".vcf", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("file", "Only .vcf files are allowed.");
            return View();
        }

        try
        {
            using Stream stream = file.OpenReadStream();
            ContactImportResult result = await _contactImportService.ImportFromVCardAsync(stream);

            TempData["SuccessMessage"] =
                $"Import successful! Added: {result.AddedCount}, Skipped: {result.SkippedCount}";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            LogErrorImportingVcf(_logger, ex);
            ModelState.AddModelError("", "An error occurred while parsing the file.");
            return View();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Export(Guid id)
    {
        try
        {
            ContactExportResult result = await _contactExportService.ExportToVCardAsync(id);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv()
    {
        ContactExportResult result = await _csvExportService.ExportContactsAsync();
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> ExportAllVCard(CancellationToken cancellationToken)
    {
        ContactExportResult result = await _contactExportService.ExportAllToVCardZipAsync(cancellationToken);
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    private static void NormalizeContactForm(ContactFormDto dto)
    {
        dto.Pronouns = dto.Pronouns == PersonalAttributeOptions.Unspecified ? null : dto.Pronouns;
        dto.Gender = dto.Gender == PersonalAttributeOptions.Unspecified ? null : dto.Gender;
        dto.Religion = string.IsNullOrWhiteSpace(dto.Religion) ? null : dto.Religion;
    }

    private static ContactEditViewModel MapToEditViewModel(ContactFormDto dto, Guid? profileImageId, bool hasRelationships = false)
    {
        return new ContactEditViewModel
        {
            Id = dto.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Nickname = dto.Nickname,
            Email = dto.Email,
            Phone = dto.Phone,
            JobTitle = dto.JobTitle,
            Company = dto.Company,
            Birthday = dto.Birthday,
            RemindOnBirthday = dto.RemindOnBirthday,
            IsHidden = dto.IsHidden,
            Pronouns = dto.Pronouns,
            Gender = dto.Gender,
            Religion = dto.Religion,
            ProfileImageId = profileImageId,
            PronounOptions = PersonalAttributeOptions.Pronouns,
            GenderOptions = PersonalAttributeOptions.Gender,
            AllLabels = dto.AllLabels,
            AssignedLabelIds = dto.AssignedLabelIds,
            HasRelationships = hasRelationships
        };
    }

    [HttpPost]
    public async Task<IActionResult> DemoteToPartial(Guid id)
    {
        if (!await _contactReadService.ContactExistsAsync(id))
        {
            return NotFound();
        }

        if (!await _contactReadService.HasRelationshipsAsync(id))
        {
            return BadRequest("Contact must have at least one relationship to be converted to a partial contact.");
        }

        ContactOperationResult result = await _contactManagementService.DemoteToPartialAsync(id);

        return result.Success
            ? RedirectToAction(nameof(Index))
            : BadRequest("Could not demote contact.");
    }
}
