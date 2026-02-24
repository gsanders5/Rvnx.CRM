using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.DTOs.DataTable;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers.Base;
using System.Globalization;

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
            bool hasContacts = await _contactReadService.HasAnyContactsAsync(showHidden);

            ContactIndexViewModel viewModel = new()
            {
                Contacts = hasContacts ? [new ContactDto()] : [],
                SuccessMessage = TempData["SuccessMessage"] as string
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> DataTable(bool showHidden = false)
        {
            DataTableRequestDto request = new()
            {
                Draw = int.TryParse(Request.Query["draw"], out int draw) ? draw : 0,
                Start = int.TryParse(Request.Query["start"], out int start) ? start : 0,
                Length = int.TryParse(Request.Query["length"], out int length) ? length : 10,
                Search = new DataTableSearchDto
                {
                    Value = Request.Query["search[value]"],
                    Regex = Request.Query["search[regex]"] == "true"
                }
            };

            int orderIndex = 0;
            while (Request.Query.ContainsKey($"order[{orderIndex}][column]"))
            {
                request.Order.Add(new DataTableOrderDto
                {
                    Column = int.Parse(Request.Query[$"order[{orderIndex}][column]"]!, CultureInfo.InvariantCulture),
                    Dir = Request.Query[$"order[{orderIndex}][dir]"]!
                });
                orderIndex++;
            }

            PagedResult<ContactDto> pagedResult = await _contactReadService.GetContactDataTableAsync(request, showHidden);

            List<ContactDataTableDto> data = new(pagedResult.Items.Count());

            foreach (ContactDto item in pagedResult.Items)
            {
                if (item == null) continue;

                ContactDataTableDto row = new()
                {
                    Id = item.Id,
                    FirstName = item.FirstName,
                    LastName = item.LastName,
                    FullName = item.FullName,
                    Company = item.Company,
                    JobTitle = item.JobTitle,
                    IsHidden = item.IsHidden,
                    ProfileImageId = item.ProfileImageId,
                    Pronouns = item.Pronouns,
                    Gender = item.Gender,
                    Religion = item.Religion,
                    IsPartial = item.IsPartial,
                    Labels = item.Labels ?? [],
                    CreatedDate = item.CreatedDate,
                    LastChangedDate = item.LastChangedDate,
                    CreatedBy = item.CreatedBy,
                    LastChangedBy = item.LastChangedBy,
                    UserId = item.UserId
                };

                // URLs
                string editUrl = Url.Action("Edit", "Contacts", new { id = item.Id }) ?? "";
                string detailsUrl = Url.Action("Details", "Contacts", new { id = item.Id }) ?? "";
                string deleteUrl = Url.Action("Delete", "Contacts", new { id = item.Id }) ?? "";
                string photoUrl = item.ProfileImageId.HasValue ? Url.Action("View", "Attachments", new { id = item.ProfileImageId }) ?? "" : "";

                string name = System.Net.WebUtility.HtmlEncode(item.FullName);

                // Photo HTML
                if (item.ProfileImageId.HasValue)
                {
                    row.PhotoHtml = $@"<div class=""d-flex align-items-center justify-content-center"" style=""width: 40px; height: 40px;"">
                                        <img src=""{photoUrl}"" class=""rounded-circle"" width=""40"" height=""40"" alt=""{name}"" style=""object-fit: cover;"" loading=""lazy"" />
                                       </div>";
                }
                else
                {
                    row.PhotoHtml = @"<div class=""d-flex align-items-center justify-content-center"" style=""width: 40px; height: 40px;"">
                                        <div class=""rounded-circle bg-secondary text-white d-flex align-items-center justify-content-center"" style=""width: 40px; height: 40px;"">
                                            <i class=""bi bi-person-fill"" aria-hidden=""true""></i>
                                        </div>
                                      </div>";
                }

                // Name HTML
                string hiddenBadge = item.IsHidden ? @"<span class=""badge bg-secondary ms-2"" title=""Hidden""><i class=""bi bi-eye-slash""></i></span>" : "";

                string labelsHtml = "";
                if (item.Labels != null && item.Labels.Any())
                {
                    labelsHtml = "<div class=\"mt-1 d-flex gap-1 flex-wrap\">";
                    foreach (var label in item.Labels)
                    {
                        string color = !string.IsNullOrEmpty(label.Color) ? label.Color : "#6c757d";
                        string labelName = System.Net.WebUtility.HtmlEncode(label.Name);
                        // Using style attribute for badge color since view does the same
                        labelsHtml += $@"<span class=""badge rounded-pill"" style=""background-color: {color}; color: #fff;"">{labelName}</span>";
                    }
                    labelsHtml += "</div>";
                }

                row.NameHtml = $@"<a href=""{detailsUrl}"" class=""fw-bold text-decoration-none"">{name}</a>{hiddenBadge}{labelsHtml}";

                // Actions HTML
                row.ActionsHtml = $@"<div class=""btn-group btn-group-sm"">
<a href=""{editUrl}"" class=""btn btn-outline-secondary"" title=""Edit"" aria-label=""Edit {name}"">
<i class=""bi bi-pencil-square""></i>
</a>
<a href=""{detailsUrl}"" class=""btn btn-outline-info"" title=""Details"" aria-label=""Details for {name}"">
<i class=""bi bi-info-circle""></i>
</a>
<a href=""{deleteUrl}"" class=""btn btn-outline-danger"" title=""Delete"" aria-label=""Delete {name}"">
<i class=""bi bi-trash""></i>
</a>
</div>";

                data.Add(row);
            }

            return Json(new DataTableResponseDto<ContactDataTableDto>
            {
                Draw = request.Draw,
                RecordsTotal = pagedResult.TotalCount,
                RecordsFiltered = pagedResult.FilteredCount,
                Data = data
            });
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
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,FirstName,LastName,Nickname,Email,Phone,JobTitle,Company,Birthday,IsHidden,Pronouns,Gender,Religion")] ContactFormDto contactDto, IFormFile? profileImage)
        {
            if (id != contactDto.Id)
            {
                return NotFound();
            }

            if (!await _contactReadService.ContactExistsAsync(id))
            {
                return NotFound();
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
