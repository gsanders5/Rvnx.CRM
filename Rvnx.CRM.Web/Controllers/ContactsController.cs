using FileTypeChecker.Web.Attributes;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Base;
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
    IFileValidationService fileValidationService,
    IImmichService immichService,
    ILabelService labelService) : AuthorizedController
{
    private const int MaxBulkIds = 1000;

    private readonly ILogger<ContactsController> _logger = logger;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IContactImportService _contactImportService = contactImportService;
    private readonly IContactExportService _contactExportService = contactExportService;
    private readonly ICsvExportService _csvExportService = csvExportService;
    private readonly IContactManagementService _contactManagementService = contactManagementService;
    private readonly IContactReadService _contactReadService = contactReadService;
    private readonly ISelfContactService _selfContactService = selfContactService;
    private readonly IFileValidationService _fileValidationService = fileValidationService;
    private readonly IImmichService _immichService = immichService;
    private readonly ILabelService _labelService = labelService;

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
            GenderOptions = PersonalAttributeOptions.Gender,
            IntroducerCandidates = await _contactReadService.GetIntroducerCandidatesAsync(null)
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
        contactDto.IntroducerCandidates = await _contactReadService.GetIntroducerCandidatesAsync(null);
        return View("Create", contactDto);
    }

    [HttpGet]
    public async Task<IActionResult> Index(bool showHidden = false)
    {
        List<ContactDto> contactDtos = await _contactReadService.GetIndexDataAsync(showHidden);
        List<LabelDto> allLabels = contactDtos.Count > 0 ? await _labelService.GetAllAsync() : [];

        ContactIndexViewModel viewModel = new()
        {
            Contacts = contactDtos,
            AllLabels = allLabels,
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
    public async Task<IActionResult> Create()
    {
        ContactCreateViewModel viewModel = new()
        {
            IsSelfCreate = false,
            PronounOptions = PersonalAttributeOptions.Pronouns,
            GenderOptions = PersonalAttributeOptions.Gender,
            IntroducerCandidates = await _contactReadService.GetIntroducerCandidatesAsync(null)
        };
        return View(viewModel);
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
        contactDto.IntroducerCandidates = await _contactReadService.GetIntroducerCandidatesAsync(null);
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
        (bool immichEnabled, IReadOnlyList<ImmichOptionDto> people, IReadOnlyList<ImmichOptionDto> tags) = await LoadImmichOptionsAsync();
        bool isSelf = await IsSelfContactAsync(id.Value);
        ContactEditViewModel viewModel = MapToEditViewModel(dto, dto.ProfileImageId, hasRelationships, immichEnabled, people, tags, isSelf);

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

        // Defense-in-depth: a user must never be able to mark their own self-contact deceased,
        // even by tampering with the form post. Doing so would silently disable their reminders,
        // dashboard, and calendar entries. Coerce the deceased fields back to a safe default.
        bool isSelf = await IsSelfContactAsync(id);
        if (isSelf)
        {
            contactDto.IsDeceased = false;
            contactDto.DateOfDeath = null;
        }

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
            contactDto.IntroducerCandidates = formConfig.IntroducerCandidates;
        }

        bool hasRelationships = await _contactReadService.HasRelationshipsAsync(id);
        (bool immichEnabled, IReadOnlyList<ImmichOptionDto> people, IReadOnlyList<ImmichOptionDto> tags) = await LoadImmichOptionsAsync();
        ContactEditViewModel viewModel = MapToEditViewModel(contactDto, formConfig?.ProfileImageId, hasRelationships, immichEnabled, people, tags, isSelf);

        return View(viewModel);
    }

    private async Task<bool> IsSelfContactAsync(Guid contactId)
    {
        Guid? selfContactId = await _selfContactService.GetSelfContactIdAsync();
        return selfContactId.HasValue && selfContactId.Value == contactId;
    }

    private async Task<(bool Enabled, IReadOnlyList<ImmichOptionDto> People, IReadOnlyList<ImmichOptionDto> Tags)> LoadImmichOptionsAsync()
    {
        if (!await _immichService.IsEnabledAsync(HttpContext.RequestAborted))
        {
            return (false, [], []);
        }

        // Cap total wait so an unreachable Immich server can't pin every Edit page render to the full HttpClient timeout.
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(3));
        Task<IReadOnlyList<ImmichOptionDto>> peopleTask = _immichService.GetAllPeopleAsync(cts.Token);
        Task<IReadOnlyList<ImmichOptionDto>> tagsTask = _immichService.GetAllTagsAsync(cts.Token);
        try
        {
            await Task.WhenAll(peopleTask, tagsTask);
            return (true, peopleTask.Result, tagsTask.Result);
        }
        catch (OperationCanceledException)
        {
            return (true,
                    peopleTask.Status == TaskStatus.RanToCompletion ? peopleTask.Result : [],
                    tagsTask.Status == TaskStatus.RanToCompletion ? tagsTask.Result : []);
        }
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
        dto.HowWeMet = string.IsNullOrWhiteSpace(dto.HowWeMet) ? null : dto.HowWeMet;
    }

    private static ContactEditViewModel MapToEditViewModel(
        ContactFormDto dto,
        Guid? profileImageId,
        bool hasRelationships = false,
        bool immichEnabled = false,
        IReadOnlyList<ImmichOptionDto>? allImmichPeople = null,
        IReadOnlyList<ImmichOptionDto>? allImmichTags = null,
        bool isSelf = false)
    {
        return new ContactEditViewModel
        {
            Id = dto.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            MaidenName = dto.MaidenName,
            Nickname = dto.Nickname,
            Email = dto.Email,
            Phone = dto.Phone,
            JobTitle = dto.JobTitle,
            Company = dto.Company,
            Birthday = dto.Birthday,
            RemindOnBirthday = dto.RemindOnBirthday,
            IsHidden = dto.IsHidden,
            IsDeceased = dto.IsDeceased,
            DateOfDeath = dto.DateOfDeath,
            Pronouns = dto.Pronouns,
            Gender = dto.Gender,
            Religion = dto.Religion,
            HowWeMet = dto.HowWeMet,
            FirstMetOn = dto.FirstMetOn,
            IntroducedByContactId = dto.IntroducedByContactId,
            ProfileImageId = profileImageId,
            ImmichPersonId = dto.ImmichPersonId,
            ImmichPersonName = dto.ImmichPersonName,
            ImmichTagId = dto.ImmichTagId,
            ImmichTagValue = dto.ImmichTagValue,
            PronounOptions = PersonalAttributeOptions.Pronouns,
            GenderOptions = PersonalAttributeOptions.Gender,
            AllLabels = dto.AllLabels,
            AssignedLabelIds = dto.AssignedLabelIds,
            IntroducerCandidates = dto.IntroducerCandidates,
            HasRelationships = hasRelationships,
            ImmichEnabled = immichEnabled,
            AllImmichPeople = allImmichPeople ?? [],
            AllImmichTags = allImmichTags ?? [],
            IsSelf = isSelf
        };
    }

    [HttpPost]
    public async Task<IActionResult> BulkDelete(Guid[] ids)
    {
        if (!TryValidateBulkIds(ids, out IActionResult? error))
        {
            return error!;
        }

        BulkOperationResult result = await _contactManagementService.BulkDeleteAsync(ids);
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> BulkSetHidden(Guid[] ids, bool hidden)
    {
        if (!TryValidateBulkIds(ids, out IActionResult? error))
        {
            return error!;
        }

        BulkOperationResult result = await _contactManagementService.BulkSetHiddenAsync(ids, hidden);
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> BulkAssignLabel(Guid[] ids, Guid labelId)
    {
        if (!TryValidateBulkIds(ids, out IActionResult? error))
        {
            return error!;
        }

        if (labelId == Guid.Empty)
        {
            return BadRequest("A label must be selected.");
        }

        BulkOperationResult result = await _labelService.BulkAssignLabelAsync(ids, labelId);
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> BulkRemoveLabel(Guid[] ids, Guid labelId)
    {
        if (!TryValidateBulkIds(ids, out IActionResult? error))
        {
            return error!;
        }

        if (labelId == Guid.Empty)
        {
            return BadRequest("A label must be selected.");
        }

        BulkOperationResult result = await _labelService.BulkRemoveLabelAsync(ids, labelId);
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> ExportSelectedCsv(Guid[] ids)
    {
        if (!TryValidateBulkIds(ids, out IActionResult? error))
        {
            return error!;
        }

        ContactExportResult result = await _csvExportService.ExportContactsAsync(ids);
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    [HttpPost]
    public async Task<IActionResult> ExportSelectedVCard(Guid[] ids, CancellationToken cancellationToken)
    {
        if (!TryValidateBulkIds(ids, out IActionResult? error))
        {
            return error!;
        }

        ContactExportResult result = await _contactExportService.ExportSelectedToVCardZipAsync(ids, cancellationToken);
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    private static bool TryValidateBulkIds(Guid[] ids, out IActionResult? error)
    {
        if (ids == null || ids.Length == 0)
        {
            error = new BadRequestObjectResult("At least one contact id is required.");
            return false;
        }
        if (ids.Length > MaxBulkIds)
        {
            error = new BadRequestObjectResult($"Too many ids; the maximum is {MaxBulkIds}.");
            return false;
        }
        error = null;
        return true;
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
