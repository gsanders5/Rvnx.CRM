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
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class ContactsController : AuthorizedController
    {
        private readonly IRepository _repository;
        private readonly ILogger<ContactsController> _logger;

        public ContactsController(IRepository repository, ILogger<ContactsController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            List<Contact> contacts = await _repository.ListAsync<Contact>();
            List<ContactDto> contactDtos = contacts.Select(c => c.ToDto()).ToList();

            // Optimally: Get all profile images for these contacts
            List<Guid> contactIds = contacts.Select(c => c.Id).ToList();

            // 1. Fetch Profile Image Attachments for these contacts
            List<Attachment> profileAttachments = await _repository.ListAsync<Attachment>(a => a.EntityType == EntityTypes.Person
                && a.AttachmentType == "ProfileImage"
                && contactIds.Contains(a.EntityId));

            if (profileAttachments.Any())
            {
                // 2. Map to DTOs
                foreach (ContactDto? dto in contactDtos)
                {
                    Attachment? attachment = profileAttachments.FirstOrDefault(a => a.EntityId == dto.Id);
                    if (attachment != null)
                    {
                        dto.ProfileImageId = attachment.Id;
                    }
                }
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

            List<RelationshipType> types = await _repository.ListAsync<RelationshipType>();

            // Relationships
            List<Relationship> relationships = await _repository.ListAsync<Relationship>(r => r.EntityId == id.Value && r.EntityType == EntityTypes.Person);
            foreach (Relationship rel in relationships)
            {
                rel.RelationshipType = types.FirstOrDefault(t => t.Id == rel.RelationshipTypeId);
                rel.RelatedPerson = await _repository.GetByIdAsync<Contact>(rel.RelatedEntityId);
            }
            contact.Relationships = relationships;

            // RelatedTo
            List<Relationship> relatedTo = await _repository.ListAsync<Relationship>(r => r.RelatedEntityId == id.Value && r.EntityType == EntityTypes.Person);
            foreach (Relationship rel in relatedTo)
            {
                rel.RelationshipType = types.FirstOrDefault(t => t.Id == rel.RelationshipTypeId);
                rel.Person = await _repository.GetByIdAsync<Contact>(rel.EntityId);
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
                // Save first to get ID (though GUID is generated in ToEntity, it's safer to ensure DB existence)
                await _repository.SaveChangesAsync();

                // Add Email
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

                // Add Phone
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

                // Add Birthday
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

            // Fetch Email (Primary)
            List<ContactMethod> emails = await _repository.ListAsync<ContactMethod>(c => c.EntityId == contact.Id && c.EntityType == EntityTypes.Person && c.Type == ContactMethodType.Email);
            // Prioritize one labeled "Primary" or just take the first one
            ContactMethod? email = emails.FirstOrDefault(e => e.Label == "Primary") ?? emails.FirstOrDefault();
            dto.Email = email?.Value;

            // Fetch Phone
            List<ContactMethod> phones = await _repository.ListAsync<ContactMethod>(c => c.EntityId == contact.Id && c.EntityType == EntityTypes.Person && c.Type == ContactMethodType.Phone);
            ContactMethod? phone = phones.FirstOrDefault(p => p.Label == "Primary") ?? phones.FirstOrDefault();
            dto.Phone = phone?.Value;

            // Fetch Birthday
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

                    // --- Update Email ---
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

                    // --- Update Phone ---
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

                    // --- Update Birthday ---
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

                    // --- Profile Image ---
                    if (profileImage != null && profileImage.Length > 0)
                    {
                        // Validate file type
                        string[] allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                        string extension = Path.GetExtension(profileImage.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(extension) || !profileImage.ContentType.StartsWith("image/"))
                        {
                            ModelState.AddModelError("", "Only image files (jpg, jpeg, png, gif) are allowed.");
                            return View(contactDto);
                        }

                        using MemoryStream ms = new();
                        await profileImage.CopyToAsync(ms);
                        byte[] fileBytes = ms.ToArray();

                        List<Attachment> existingAttachments = await _repository.ListAsync<Attachment>(a => a.EntityId == id && a.EntityType == EntityTypes.Person && a.AttachmentType == "ProfileImage");
                        Attachment? existingAttachment = existingAttachments.FirstOrDefault();

                        if (existingAttachment != null)
                        {
                            // Load content to update it
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
            await DeleteContactDependenciesAsync(id);
            await _repository.DeleteAsync<Contact>(id);
            await _repository.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Manually deletes related generic entities.
        /// Due to polymorphic relationships (EntityId/EntityType), these cannot be handled by database cascade deletes.
        /// </summary>
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

            // Relationships where this contact is the Source
            List<Relationship> relationships = await _repository.ListAsync<Relationship>(r => r.EntityId == contactId && r.EntityType == EntityTypes.Person);
            if (relationships.Any()) await _repository.DeleteRangeAsync(relationships);

            // Relationships where this contact is the Target
            List<Relationship> relatedTo = await _repository.ListAsync<Relationship>(r => r.RelatedEntityId == contactId && r.EntityType == EntityTypes.Person);
            if (relatedTo.Any()) await _repository.DeleteRangeAsync(relatedTo);

            // PhoneNumbers
            List<PhoneNumber> phoneNumbers = await _repository.ListAsync<PhoneNumber>(p => p.EntityId == contactId && p.EntityType == EntityTypes.Person);
            if (phoneNumbers.Any()) await _repository.DeleteRangeAsync(phoneNumbers);
        }

        private async Task<bool> ContactExists(Guid id)
        {
            return await _repository.ExistsAsync<Contact>(id);
        }
    }
}
