using FileTypeChecker.Web.Attributes;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class ContactsController(ILogger<ContactsController> logger, ICurrentUserService currentUserService, IContactImportService contactImportService, IContactExportService contactExportService, IContactManagementService contactManagementService, IContactReadService contactReadService, ISelfContactService selfContactService, IFileValidationService fileValidationService) : AuthorizedController
    {
        private readonly ILogger<ContactsController> _logger = logger;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        private readonly IContactImportService _contactImportService = contactImportService;
        private readonly IContactExportService _contactExportService = contactExportService;
        private readonly IContactManagementService _contactManagementService = contactManagementService;
        private readonly IContactReadService _contactReadService = contactReadService;
        private readonly ISelfContactService _selfContactService = selfContactService;
        private readonly IFileValidationService _fileValidationService = fileValidationService;

        private static readonly Action<ILogger, Exception?> LogErrorImportingVcf =
            LoggerMessage.Define(
                LogLevel.Error,
                new EventId(1, nameof(LogErrorImportingVcf)),
                "Error importing VCF");

        public async Task<IActionResult> Self()
        {
            if (!_currentUserService.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            Guid? selfContactId = await _selfContactService.GetSelfContactIdAsync(HttpContext.User);

            return selfContactId.HasValue
                ? RedirectToAction(nameof(Details), new { id = selfContactId })
                : RedirectToAction(nameof(CreateSelf));
        }

        public async Task<IActionResult> CreateSelf()
        {
            if (!_currentUserService.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            Guid? selfContactId = await _selfContactService.GetSelfContactIdAsync(HttpContext.User);

            if (selfContactId.HasValue)
            {
                return RedirectToAction(nameof(Details), new { id = selfContactId });
            }

            ContactFormDto? dto = await _selfContactService.GetSelfContactFormAsync(HttpContext.User);
            if (dto == null)
            {
                return RedirectToAction("Index");
            }

            ContactCreateViewModel viewModel = new()
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Nickname = dto.Nickname,
                Email = dto.Email,
                Phone = dto.Phone,
                JobTitle = dto.JobTitle,
                Company = dto.Company,
                Birthday = dto.Birthday,
                IsHidden = dto.IsHidden,
                Pronouns = dto.Pronouns,
                Gender = dto.Gender,
                Religion = dto.Religion,
                IsSelfCreate = true,
                PronounOptions = PersonalAttributeOptions.Pronouns,
                GenderOptions = PersonalAttributeOptions.Gender
            };

            return View("Create", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSelf([Bind("FirstName,LastName,Nickname,Email,Phone,JobTitle,Company,Birthday,Pronouns,Gender,Religion")] ContactCreateViewModel contactDto)
        {
            if (!_currentUserService.IsAuthenticated)
            {
                return Unauthorized();
            }

            contactDto.Pronouns = contactDto.Pronouns == "Unspecified" ? null : contactDto.Pronouns;
            contactDto.Gender = contactDto.Gender == "Unspecified" ? null : contactDto.Gender;
            contactDto.Religion = string.IsNullOrWhiteSpace(contactDto.Religion) ? null : contactDto.Religion;

            if (ModelState.IsValid)
            {
                ContactOperationResult result = await _selfContactService.CreateSelfContactAsync(HttpContext.User, contactDto);
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

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            ContactDetailDto? contactDto = await _contactReadService.GetContactDetailsAsync(id.Value);
            return contactDto == null ? NotFound() : View(contactDto);
        }

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,Nickname,Email,Phone,JobTitle,Company,Birthday,IsHidden,Pronouns,Gender,Religion")] ContactCreateViewModel contactDto)
        {
            contactDto.Pronouns = contactDto.Pronouns == "Unspecified" ? null : contactDto.Pronouns;
            contactDto.Gender = contactDto.Gender == "Unspecified" ? null : contactDto.Gender;
            contactDto.Religion = string.IsNullOrWhiteSpace(contactDto.Religion) ? null : contactDto.Religion;

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

            ContactEditViewModel viewModel = new()
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
                IsHidden = dto.IsHidden,
                Pronouns = dto.Pronouns,
                Gender = dto.Gender,
                Religion = dto.Religion,
                PronounOptions = PersonalAttributeOptions.Pronouns,
                GenderOptions = PersonalAttributeOptions.Gender,
                AllLabels = dto.AllLabels,
                AssignedLabelIds = dto.AssignedLabelIds
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,FirstName,LastName,Nickname,Email,Phone,JobTitle,Company,Birthday,IsHidden,Pronouns,Gender,Religion")] ContactFormDto contactDto, [AllowImages] IFormFile? profileImage)
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

            contactDto.Pronouns = contactDto.Pronouns == "Unspecified" ? null : contactDto.Pronouns;
            contactDto.Gender = contactDto.Gender == "Unspecified" ? null : contactDto.Gender;
            contactDto.Religion = string.IsNullOrWhiteSpace(contactDto.Religion) ? null : contactDto.Religion;

            if (ModelState.IsValid)
            {
                Stream? stream = null;
                if (profileImage != null && profileImage.Length > 0)
                {
                    stream = profileImage.OpenReadStream();
                }

                using (stream)
                {
                    ContactOperationResult result = await _contactManagementService.UpdateContactAsync(id, contactDto, stream, profileImage?.FileName, profileImage?.ContentType);

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
            }

            ContactFormDto? formConfig = await _contactReadService.GetContactFormAsync(id);
            if (formConfig != null)
            {
                contactDto.AllLabels = formConfig.AllLabels;
                contactDto.AssignedLabelIds = formConfig.AssignedLabelIds;
            }

            ContactEditViewModel viewModel = new()
            {
                Id = contactDto.Id,
                FirstName = contactDto.FirstName,
                LastName = contactDto.LastName,
                Nickname = contactDto.Nickname,
                Email = contactDto.Email,
                Phone = contactDto.Phone,
                JobTitle = contactDto.JobTitle,
                Company = contactDto.Company,
                Birthday = contactDto.Birthday,
                IsHidden = contactDto.IsHidden,
                Pronouns = contactDto.Pronouns,
                Gender = contactDto.Gender,
                Religion = contactDto.Religion,
                PronounOptions = PersonalAttributeOptions.Pronouns,
                GenderOptions = PersonalAttributeOptions.Gender,
                AllLabels = contactDto.AllLabels,
                AssignedLabelIds = contactDto.AssignedLabelIds
            };

            return View(viewModel);
        }

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _contactManagementService.DeleteContactAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignLabel(Guid contactId, Guid labelId, [FromServices] ILabelService labelService, string? returnUrl = null)
        {
            if (contactId != Guid.Empty && labelId != Guid.Empty)
            {
                await labelService.AssignLabelAsync(contactId, labelId);
            }

            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction(nameof(Edit), new { id = contactId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLabel(Guid contactId, Guid labelId, [FromServices] ILabelService labelService, string? returnUrl = null)
        {
            if (contactId != Guid.Empty && labelId != Guid.Empty)
            {
                await labelService.RemoveLabelAsync(contactId, labelId);
            }

            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction(nameof(Edit), new { id = contactId });
        }

        public IActionResult Import()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

                TempData["SuccessMessage"] = $"Import successful! Added: {result.AddedCount}, Skipped: {result.SkippedCount}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                LogErrorImportingVcf(_logger, ex);
                ModelState.AddModelError("", "An error occurred while parsing the file.");
                return View();
            }
        }

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
    }
}
