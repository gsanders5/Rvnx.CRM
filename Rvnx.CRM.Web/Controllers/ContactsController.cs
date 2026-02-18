using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class ContactsController : AuthorizedController
    {
        private readonly IRepository _repository;
        private readonly ILogger<ContactsController> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IVCardService _vCardService;
        private readonly IFileValidationService _fileValidationService;

        public ContactsController(IRepository repository, ILogger<ContactsController> logger, ICurrentUserService currentUserService, IVCardService vCardService, IFileValidationService fileValidationService)
        {
            _repository = repository;
            _logger = logger;
            _currentUserService = currentUserService;
            _vCardService = vCardService;
            _fileValidationService = fileValidationService;
        }

        public async Task<IActionResult> Self()
        {
            if (!_currentUserService.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            string? userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Index", "Home");
            }

            Rvnx.CRM.Core.Models.User? user = (await _repository.ListAsync<Rvnx.CRM.Core.Models.User>(u => u.SubjectId == userId)).FirstOrDefault();

            if (user == null)
            {
                return RedirectToAction("Index");
            }

            if (user.SelfContactId.HasValue)
            {
                return RedirectToAction(nameof(Details), new { id = user.SelfContactId });
            }

            return RedirectToAction(nameof(CreateSelf));
        }

        public async Task<IActionResult> CreateSelf()
        {
            if (!_currentUserService.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            string? userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Index", "Home");

            Rvnx.CRM.Core.Models.User? user = (await _repository.ListAsync<Rvnx.CRM.Core.Models.User>(u => u.SubjectId == userId)).FirstOrDefault();
            if (user == null) return RedirectToAction("Index");

            if (user.SelfContactId.HasValue)
            {
                return RedirectToAction(nameof(Details), new { id = user.SelfContactId });
            }

            CreateContactDto dto = new()
            {
                Email = user.Email,
                FirstName = _currentUserService.UserName ?? string.Empty
            };

            ViewBag.IsSelfCreate = true;
            return View("Create", dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSelf([Bind("FirstName,LastName,Nickname,Email,Phone,JobTitle,Company,Birthday")] CreateContactDto contactDto)
        {
            if (!_currentUserService.IsAuthenticated) return Unauthorized();

            string? userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            Rvnx.CRM.Core.Models.User? user = (await _repository.ListAsync<Rvnx.CRM.Core.Models.User>(u => u.SubjectId == userId)).FirstOrDefault();
            if (user == null) return RedirectToAction("Index");

            if (user.SelfContactId.HasValue)
            {
                return RedirectToAction(nameof(Details), new { id = user.SelfContactId });
            }

            if (ModelState.IsValid)
            {
                Contact contact = contactDto.ToEntity();

                await _repository.AddAsync(contact);

                user.SelfContactId = contact.Id;
                await _repository.UpdateAsync(user);

                await _repository.SaveChangesAsync();

                if (!string.IsNullOrEmpty(contactDto.Email))
                {
                    await _repository.AddAsync(new ContactMethod
                    {
                        Id = Guid.NewGuid(),
                        EntityId = contact.Id,
                        EntityType = EntityTypes.Person,
                        Type = ContactMethodType.Email,
                        Value = contactDto.Email,
                        Label = "Primary"
                    });
                }

                if (!string.IsNullOrEmpty(contactDto.Phone))
                {
                    await _repository.AddAsync(new ContactMethod
                    {
                        Id = Guid.NewGuid(),
                        EntityId = contact.Id,
                        EntityType = EntityTypes.Person,
                        Type = ContactMethodType.Phone,
                        Value = contactDto.Phone,
                        Label = "Primary"
                    });
                }

                if (contactDto.Birthday.HasValue)
                {
                    await _repository.AddAsync(new SignificantDate
                    {
                        Id = Guid.NewGuid(),
                        EntityId = contact.Id,
                        EntityType = EntityTypes.Person,
                        Title = "Birthday",
                        Date = contactDto.Birthday.Value,
                        Description = "Birthday",
                        RemindMe = true,
                        EventFrequency = TimeSpan.FromDays(365)
                    });
                }

                await _repository.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = contact.Id });
            }

            ViewBag.IsSelfCreate = true;
            return View("Create", contactDto);
        }

        public async Task<IActionResult> Index()
        {
            List<Contact> contacts = await _repository.ListAsync<Contact>();
            List<ContactDto> contactDtos = contacts.Select(c => c.ToDto()).ToList();

            List<Guid> contactIds = contacts.Select(c => c.Id).ToList();

            List<Attachment> profileAttachments = await _repository.ListAsync<Attachment>(a => a.EntityType == EntityTypes.Person
                && a.AttachmentType == "ProfileImage"
                && contactIds.Contains(a.EntityId));
                
                
            if (profileAttachments != null && profileAttachments.Any())
            {
                var attachmentMap = profileAttachments
                    .Where(a => a != null)
                    .GroupBy(a => a.EntityId) // Handle potential duplicates gracefully
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (ContactDto? dto in contactDtos)
                {
                    if (dto != null && attachmentMap.TryGetValue(dto.Id, out Attachment? attachment))
                    {
                        dto.ProfileImageId = attachment.Id;
                    }
                }
            }

            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
            }

            return View(contactDtos);
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            Contact? contact = await _repository.GetByIdWithIncludesAsync<Contact>(id.Value, "Employers");
            if (contact == null) return NotFound();


            contact.Notes = await _repository.ListAsync<Note>(n => n.EntityId == id.Value && n.EntityType == EntityTypes.Person);
            contact.Reminders = await _repository.ListAsync<Reminder>(r => r.EntityId == id.Value && r.EntityType == EntityTypes.Person);
            contact.SignificantDates = await _repository.ListAsync<SignificantDate>(d => d.EntityId == id.Value && d.EntityType == EntityTypes.Person);

            // Relationships
            List<Relationship> relationships = await _repository.ListAsync<Relationship>(r => r.EntityId == id.Value && r.EntityType == EntityTypes.Person);

            // RelatedTo
            List<Relationship> relatedTo = await _repository.ListAsync<Relationship>(r => r.RelatedEntityId == id.Value && r.EntityType == EntityTypes.Person);

            // Fetch all related contacts in one go
            List<Guid> relatedIds = relationships.Select(r => r.RelatedEntityId)
                .Concat(relatedTo.Select(r => r.EntityId))
                .Distinct()
                .ToList();

            List<Contact> relatedContacts = new();
            if (relatedIds.Any())
            {
                relatedContacts = await _repository.ListAsync<Contact>(c => relatedIds.Contains(c.Id));
            }

            // Manually populate navigation properties for display (since we don't have LazyLoading or Include for generic relationship yet)
            // And use static service to fill relationship names (or rely on NotMapped properties)
            foreach (Relationship rel in relationships)
            {
                // No DB lookup for type anymore. Property `RelationshipTypeName` etc. handles it.
                rel.RelatedPerson = relatedContacts.FirstOrDefault(c => c.Id == rel.RelatedEntityId);
            }
            contact.Relationships = relationships;

            foreach (Relationship rel in relatedTo)
            {
                rel.Person = relatedContacts.FirstOrDefault(c => c.Id == rel.EntityId);
            }
            contact.RelatedTo = relatedTo;

            // Pets
            List<Pet> pets = await _repository.ListAsync<Pet>(p => p.EntityId == id.Value && p.EntityType == EntityTypes.Person);

            // Contact Infos
            contact.ContactMethods = await _repository.ListAsync<ContactMethod>(i => i.EntityId == id.Value && i.EntityType == EntityTypes.Person);

            // Facts
            contact.Facts = await _repository.ListAsync<Fact>(f => f.EntityId == id.Value && f.EntityType == EntityTypes.Person);

            // Attachments
            contact.Attachments = await _repository.ListAsync<Attachment>(a => a.EntityId == id.Value && a.EntityType == EntityTypes.Person && a.AttachmentType != "ProfileImage");

            ContactDetailDto contactDto = contact.ToDetailDto();
            contactDto.Pets = pets.Select(p => p.ToDto()).ToList();

            // Profile Image
            List<Attachment> profileAttachments = await _repository.ListAsync<Attachment>(a => a.EntityId == id.Value && a.EntityType == EntityTypes.Person && a.AttachmentType == "ProfileImage");
            Attachment? profileAttachment = profileAttachments.FirstOrDefault();

            if (profileAttachment != null)
            {
                contactDto.ProfileImageId = profileAttachment.Id;
            }

            return View(contactDto);
        }

        public IActionResult Create()
        {
            return View(new CreateContactDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,Nickname,Email,Phone,JobTitle,Company,Birthday")] CreateContactDto contactDto)
        {
            if (ModelState.IsValid)
            {
                Contact contact = contactDto.ToEntity();
                await _repository.AddAsync(contact);
                await _repository.SaveChangesAsync();

                if (!string.IsNullOrEmpty(contactDto.Email))
                {
                    await _repository.AddAsync(new ContactMethod
                    {
                        Id = Guid.NewGuid(),
                        EntityId = contact.Id,
                        EntityType = EntityTypes.Person,
                        Type = ContactMethodType.Email,
                        Value = contactDto.Email,
                        Label = "Primary"
                    });
                }

                if (!string.IsNullOrEmpty(contactDto.Phone))
                {
                    await _repository.AddAsync(new ContactMethod
                    {
                        Id = Guid.NewGuid(),
                        EntityId = contact.Id,
                        EntityType = EntityTypes.Person,
                        Type = ContactMethodType.Phone,
                        Value = contactDto.Phone,
                        Label = "Primary"
                    });
                }

                if (contactDto.Birthday.HasValue)
                {
                    await _repository.AddAsync(new SignificantDate
                    {
                        Id = Guid.NewGuid(),
                        EntityId = contact.Id,
                        EntityType = EntityTypes.Person,
                        Title = "Birthday",
                        Date = contactDto.Birthday.Value,
                        Description = "Birthday",
                        RemindMe = true,
                        EventFrequency = TimeSpan.FromDays(365)
                    });
                }

                await _repository.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(contactDto);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            Contact? contact = await _repository.GetByIdAsync<Contact>(id.Value);
            if (contact == null) return NotFound();

            UpdateContactDto dto = new()
            {
                Id = contact.Id,
                FirstName = contact.FirstName,
                LastName = contact.LastName ?? string.Empty,
                Nickname = contact.Nickname,
                JobTitle = contact.JobTitle,
                Company = contact.Company
            };

            List<ContactMethod> emails = await _repository.ListAsync<ContactMethod>(c => c.EntityId == contact.Id && c.EntityType == EntityTypes.Person && c.Type == ContactMethodType.Email);
            ContactMethod? email = emails.FirstOrDefault(e => e.Label == "Primary") ?? emails.FirstOrDefault();
            dto.Email = email?.Value;

            List<ContactMethod> phones = await _repository.ListAsync<ContactMethod>(c => c.EntityId == contact.Id && c.EntityType == EntityTypes.Person && c.Type == ContactMethodType.Phone);
            ContactMethod? phone = phones.FirstOrDefault(p => p.Label == "Primary") ?? phones.FirstOrDefault();
            dto.Phone = phone?.Value;

            List<SignificantDate> bdays = await _repository.ListAsync<SignificantDate>(d => d.EntityId == contact.Id && d.EntityType == EntityTypes.Person && d.Title == "Birthday");
            dto.Birthday = bdays.FirstOrDefault()?.Date;

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,FirstName,LastName,Nickname,Email,Phone,JobTitle,Company,Birthday")] UpdateContactDto contactDto, IFormFile? profileImage)
        {
            if (id != contactDto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    Contact? existingContact = await _repository.GetByIdAsync<Contact>(id);
                    if (existingContact == null) return NotFound();

                    existingContact.UpdateEntity(contactDto);

                    List<ContactMethod> emails = await _repository.ListAsync<ContactMethod>(c => c.EntityId == id && c.EntityType == EntityTypes.Person && c.Type == ContactMethodType.Email);
                    ContactMethod? existingEmail = emails.FirstOrDefault(e => e.Label == "Primary") ?? emails.FirstOrDefault();

                    if (!string.IsNullOrEmpty(contactDto.Email))
                    {
                        if (existingEmail != null)
                        {
                            existingEmail.Value = contactDto.Email;
                            await _repository.UpdateAsync(existingEmail);
                        }
                        else
                        {
                            await _repository.AddAsync(new ContactMethod
                            {
                                Id = Guid.NewGuid(),
                                EntityId = id,
                                EntityType = EntityTypes.Person,
                                Type = ContactMethodType.Email,
                                Value = contactDto.Email,
                                Label = "Primary"
                            });
                        }
                    }
                    else if (existingEmail != null)
                    {
                        await _repository.DeleteAsync<ContactMethod>(existingEmail.Id);
                    }

                    List<ContactMethod> phones = await _repository.ListAsync<ContactMethod>(c => c.EntityId == id && c.EntityType == EntityTypes.Person && c.Type == ContactMethodType.Phone);
                    ContactMethod? existingPhone = phones.FirstOrDefault(p => p.Label == "Primary") ?? phones.FirstOrDefault();

                    if (!string.IsNullOrEmpty(contactDto.Phone))
                    {
                        if (existingPhone != null)
                        {
                            existingPhone.Value = contactDto.Phone;
                            await _repository.UpdateAsync(existingPhone);
                        }
                        else
                        {
                            await _repository.AddAsync(new ContactMethod
                            {
                                Id = Guid.NewGuid(),
                                EntityId = id,
                                EntityType = EntityTypes.Person,
                                Type = ContactMethodType.Phone,
                                Value = contactDto.Phone,
                                Label = "Primary"
                            });
                        }
                    }
                    else if (existingPhone != null)
                    {
                        await _repository.DeleteAsync<ContactMethod>(existingPhone.Id);
                    }

                    List<SignificantDate> bdays = await _repository.ListAsync<SignificantDate>(d => d.EntityId == id && d.EntityType == EntityTypes.Person && d.Title == "Birthday");
                    SignificantDate? existingBday = bdays.FirstOrDefault();

                    if (contactDto.Birthday.HasValue)
                    {
                        if (existingBday != null)
                        {
                            existingBday.Date = contactDto.Birthday.Value;
                            await _repository.UpdateAsync(existingBday);
                        }
                        else
                        {
                            await _repository.AddAsync(new SignificantDate
                            {
                                Id = Guid.NewGuid(),
                                EntityId = id,
                                EntityType = EntityTypes.Person,
                                Title = "Birthday",
                                Date = contactDto.Birthday.Value,
                                Description = "Birthday",
                                RemindMe = true,
                                EventFrequency = TimeSpan.FromDays(365)
                            });
                        }
                    }
                    else if (existingBday != null)
                    {
                        await _repository.DeleteAsync<SignificantDate>(existingBday.Id);
                    }

                    if (profileImage != null && profileImage.Length > 0)
                    {
                        string extension = Path.GetExtension(profileImage.FileName).ToLowerInvariant();
                        if (!_fileValidationService.IsImageExtension(extension) || !profileImage.ContentType.StartsWith("image/"))
                        {
                            ModelState.AddModelError("", "Only image files (jpg, jpeg, png, gif) are allowed.");
                            return View(contactDto);
                        }

                        using MemoryStream ms = new();
                        await profileImage.CopyToAsync(ms);
                        byte[] fileBytes = ms.ToArray();

                        if (!_fileValidationService.IsValidImageSignature(fileBytes, extension))
                        {
                            ModelState.AddModelError("", "Invalid file signature.");
                            return View(contactDto);
                        }

                        List<Attachment> existingAttachments = await _repository.ListAsync<Attachment>(a => a.EntityId == id && a.EntityType == EntityTypes.Person && a.AttachmentType == "ProfileImage");
                        Attachment? existingAttachment = existingAttachments.FirstOrDefault();

                        if (existingAttachment != null)
                        {
                            existingAttachment = await _repository.GetByIdWithIncludesAsync<Attachment>(existingAttachment.Id, "AttachmentContent");
                            if (existingAttachment != null)
                            {
                                existingAttachment.AttachmentContent ??= new AttachmentContent { AttachmentId = existingAttachment.Id };
                                existingAttachment.AttachmentContent.Content = fileBytes;

                                existingAttachment.ContentType = profileImage.ContentType;
                                existingAttachment.FileName = profileImage.FileName;
                                await _repository.UpdateAsync(existingAttachment);
                            }
                        }
                        else
                        {
                            Attachment attachment = new()
                            {
                                Id = Guid.NewGuid(),
                                EntityId = id,
                                EntityType = EntityTypes.Person,
                                AttachmentType = "ProfileImage",
                                ContentType = profileImage.ContentType,
                                FileName = profileImage.FileName,
                                AttachmentContent = new AttachmentContent
                                {
                                    Content = fileBytes
                                }
                            };
                            await _repository.AddAsync(attachment);
                        }
                    }

                    await _repository.UpdateAsync(existingContact);
                    await _repository.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await ContactExists(contactDto.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(contactDto);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();
            Contact? contact = await _repository.GetByIdAsync<Contact>(id.Value);
            return contact == null ? NotFound() : View(contact);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            List<Rvnx.CRM.Core.Models.User> userWithSelfContact = await _repository.ListAsync<Rvnx.CRM.Core.Models.User>(u => u.SelfContactId == id);
            foreach (Rvnx.CRM.Core.Models.User user in userWithSelfContact)
            {
                user.SelfContactId = null;
                await _repository.UpdateAsync(user);
            }

            await DeleteContactDependenciesAsync(id);
            await _repository.DeleteAsync<Contact>(id);
            await _repository.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task DeleteContactDependenciesAsync(Guid contactId)
        {
            List<Note> notes = await _repository.ListAsync<Note>(n => n.EntityId == contactId && n.EntityType == EntityTypes.Person);
            if (notes.Any()) await _repository.DeleteRangeAsync(notes);

            List<Reminder> reminders = await _repository.ListAsync<Reminder>(r => r.EntityId == contactId && r.EntityType == EntityTypes.Person);
            if (reminders.Any()) await _repository.DeleteRangeAsync(reminders);

            List<SignificantDate> importantDates = await _repository.ListAsync<SignificantDate>(d => d.EntityId == contactId && d.EntityType == EntityTypes.Person);
            if (importantDates.Any()) await _repository.DeleteRangeAsync(importantDates);

            List<Pet> pets = await _repository.ListAsync<Pet>(p => p.EntityId == contactId && p.EntityType == EntityTypes.Person);
            if (pets.Any()) await _repository.DeleteRangeAsync(pets);

            List<ContactMethod> contactInfos = await _repository.ListAsync<ContactMethod>(i => i.EntityId == contactId && i.EntityType == EntityTypes.Person);
            if (contactInfos.Any()) await _repository.DeleteRangeAsync(contactInfos);

            List<Fact> facts = await _repository.ListAsync<Fact>(f => f.EntityId == contactId && f.EntityType == EntityTypes.Person);
            if (facts.Any()) await _repository.DeleteRangeAsync(facts);

            List<Address> addresses = await _repository.ListAsync<Address>(a => a.EntityId == contactId && a.EntityType == EntityTypes.Person);
            if (addresses.Any()) await _repository.DeleteRangeAsync(addresses);

            List<Attachment> attachments = await _repository.ListAsync<Attachment>(a => a.EntityId == contactId && a.EntityType == EntityTypes.Person);
            if (attachments.Any()) await _repository.DeleteRangeAsync(attachments);

            List<Relationship> relationships = await _repository.ListAsync<Relationship>(r => r.EntityId == contactId && r.EntityType == EntityTypes.Person);
            if (relationships.Any()) await _repository.DeleteRangeAsync(relationships);

            List<Relationship> relatedTo = await _repository.ListAsync<Relationship>(r => r.RelatedEntityId == contactId && r.EntityType == EntityTypes.Person);
            if (relatedTo.Any()) await _repository.DeleteRangeAsync(relatedTo);

            List<PhoneNumber> phoneNumbers = await _repository.ListAsync<PhoneNumber>(p => p.EntityId == contactId && p.EntityType == EntityTypes.Person);
            if (phoneNumbers.Any()) await _repository.DeleteRangeAsync(phoneNumbers);
        }

        private async Task<bool> ContactExists(Guid id)
        {
            return await _repository.ExistsAsync<Contact>(id);
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
                IEnumerable<Contact> importedContacts = _vCardService.ParseVCard(stream);

                int addedCount = 0;
                int skippedCount = 0;

                foreach (var contact in importedContacts)
                {
                    if (await IsDuplicateAsync(contact))
                    {
                        skippedCount++;
                        continue;
                    }

                    // Note: ContactMethods and SignificantDates are [NotMapped] collections on the Contact (Person) entity.
                    // EF Core's AddAsync(contact) will NOT recursively add these entities because they are not navigation properties mapped to the DB.
                    // We must add them explicitly and link them to the Contact ID.
                    await _repository.AddAsync(contact);
                    await _repository.SaveChangesAsync(); // Save to generate ID and allow next duplicate checks to find it if file has dupes

                    if (contact.ContactMethods != null)
                    {
                        foreach(var cm in contact.ContactMethods)
                        {
                            cm.EntityId = contact.Id;
                            await _repository.AddAsync(cm);
                        }
                    }

                    if (contact.SignificantDates != null)
                    {
                        foreach(var sd in contact.SignificantDates)
                        {
                            sd.EntityId = contact.Id;
                            await _repository.AddAsync(sd);
                        }
                    }

                    await _repository.SaveChangesAsync();
                    addedCount++;
                }

                TempData["SuccessMessage"] = $"Import successful! Added: {addedCount}, Skipped: {skippedCount}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing VCF");
                ModelState.AddModelError("", "An error occurred while parsing the file.");
                return View();
            }
        }

        private async Task<bool> IsDuplicateAsync(Contact candidate)
        {
            var existingNames = await _repository.ListAsync<Contact>(c => c.FirstName == candidate.FirstName && c.LastName == candidate.LastName);
            if (existingNames.Any()) return true;

            if (candidate.ContactMethods != null && candidate.ContactMethods.Any())
            {
                var valuesToCheck = candidate.ContactMethods.Select(m => m.Value).ToList();
                if (valuesToCheck.Any())
                {
                    var existingMethods = await _repository.ListAsync<ContactMethod>(cm =>
                        cm.EntityType == EntityTypes.Person &&
                        valuesToCheck.Contains(cm.Value));

                    if (existingMethods.Any()) return true;
                }
            }

            return false;
        }

        public async Task<IActionResult> Export(Guid id)
        {
            var contact = await _repository.GetByIdAsync<Contact>(id);
            if (contact == null) return NotFound();

            contact.ContactMethods = await _repository.ListAsync<ContactMethod>(c => c.EntityId == id && c.EntityType == EntityTypes.Person);
            contact.SignificantDates = await _repository.ListAsync<SignificantDate>(d => d.EntityId == id && d.EntityType == EntityTypes.Person);

            byte[] vcfBytes = _vCardService.ExportVCard(contact);
            string fileName = $"{contact.FirstName}_{contact.LastName}.vcf";
            return File(vcfBytes, "text/vcard", fileName);
        }
    }
}
