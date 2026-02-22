using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class ContactsController(ILogger<ContactsController> logger, ICurrentUserService currentUserService, IContactImportService contactImportService, IContactExportService contactExportService, IContactManagementService contactManagementService, IContactReadService contactReadService, ISelfContactService selfContactService) : AuthorizedController
    {
        private readonly ILogger<ContactsController> _logger = logger;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        private readonly IContactImportService _contactImportService = contactImportService;
        private readonly IContactExportService _contactExportService = contactExportService;
        private readonly IContactManagementService _contactManagementService = contactManagementService;
        private readonly IContactReadService _contactReadService = contactReadService;
        private readonly ISelfContactService _selfContactService = selfContactService;

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
            if (dto == null) return RedirectToAction("Index");

            var viewModel = new ContactCreateViewModel
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
                IsSelfCreate = true
            };

            return View("Create", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSelf([Bind("FirstName,LastName,Nickname,Email,Phone,JobTitle,Company,Birthday")] ContactCreateViewModel contactDto)
        {
            if (!_currentUserService.IsAuthenticated) return Unauthorized();

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
            return View("Create", contactDto);
        }

        public async Task<IActionResult> Index(bool showHidden = false)
        {
            List<ContactDto> contactDtos = await _contactReadService.GetIndexDataAsync(showHidden);

            var viewModel = new ContactIndexViewModel
            {
                Contacts = contactDtos,
                SuccessMessage = TempData["SuccessMessage"] as string
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            ContactDetailDto? contactDto = await _contactReadService.GetContactDetailsAsync(id.Value);
            return contactDto == null ? NotFound() : View(contactDto);
        }

        public IActionResult Create()
        {
            return View(new ContactCreateViewModel { IsSelfCreate = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,Nickname,Email,Phone,JobTitle,Company,Birthday,IsHidden")] ContactCreateViewModel contactDto)
        {
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
            return View(contactDto);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            ContactFormDto? dto = await _contactReadService.GetContactFormAsync(id.Value);
            return dto == null ? NotFound() : View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,FirstName,LastName,Nickname,Email,Phone,JobTitle,Company,Birthday,IsHidden")] ContactFormDto contactDto, IFormFile? profileImage)
        {
            if (id != contactDto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
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

                        foreach (string error in result.Errors)
                        {
                            ModelState.AddModelError("", error);
                        }
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _contactReadService.ContactExistsAsync(id)) return NotFound();
                    else throw;
                }
            }
            return View(contactDto);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();
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
                _logger.LogError(ex, "Error importing VCF");
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
