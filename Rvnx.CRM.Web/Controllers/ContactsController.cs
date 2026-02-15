using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;

namespace Rvnx.CRM.Web.Controllers
{
    public class ContactsController : Controller
    {
        private readonly IRepository _repository;
        private readonly ILogger<ContactsController> _logger;

        public ContactsController(IRepository repository, ILogger<ContactsController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // GET: Contacts
        public async Task<IActionResult> Index()
        {
            var contacts = await _repository.ListAsync<Contact>();
            return View(contacts.Select(c => c.ToDto()).ToList());
        }

        // GET: Contacts/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var contact = await _repository.GetByIdWithIncludesAsync<Contact>(id.Value, "Employers");
            if (contact == null) return NotFound();


            contact.Notes = await _repository.ListAsync<Note>(n => n.EntityId == id.Value && n.EntityType == EntityTypes.Person);
            contact.Reminders = await _repository.ListAsync<Reminder>(r => r.EntityId == id.Value && r.EntityType == EntityTypes.Person);
            contact.ImportantDates = await _repository.ListAsync<ImportantDate>(d => d.EntityId == id.Value && d.EntityType == EntityTypes.Person);

            var types = await _repository.ListAsync<RelationshipType>();

            // Relationships
            var relationships = await _repository.ListAsync<Relationship>(r => r.EntityId == id.Value && r.EntityType == EntityTypes.Person);
            foreach (var rel in relationships)
            {
                rel.RelationshipType = types.FirstOrDefault(t => t.Id == rel.RelationshipTypeId);
                rel.RelatedPerson = await _repository.GetByIdAsync<Contact>(rel.RelatedEntityId);
            }
            contact.Relationships = relationships;

            // RelatedTo
            var relatedTo = await _repository.ListAsync<Relationship>(r => r.RelatedEntityId == id.Value && r.EntityType == EntityTypes.Person);
            foreach (var rel in relatedTo)
            {
                rel.RelationshipType = types.FirstOrDefault(t => t.Id == rel.RelationshipTypeId);
                rel.Person = await _repository.GetByIdAsync<Contact>(rel.EntityId);
            }
            contact.RelatedTo = relatedTo;

            // Pets
            var pets = await _repository.ListAsync<Pet>(p => p.EntityId == id.Value && p.EntityType == EntityTypes.Person);

            // Contact Infos
            contact.ContactInfos = await _repository.ListAsync<ContactInfo>(i => i.EntityId == id.Value && i.EntityType == EntityTypes.Person);

            // Facts
            contact.Facts = await _repository.ListAsync<Fact>(f => f.EntityId == id.Value && f.EntityType == EntityTypes.Person);

            var contactDto = contact.ToDetailDto();
            contactDto.Pets = pets.Select(p => p.ToDto()).ToList();

            // Profile Image
            var profileAttachments = await _repository.ListAsync<Attachment>(a => a.EntityId == id.Value && a.EntityType == EntityTypes.Person && a.AttachmentType == "ProfileImage");
            var profileAttachment = profileAttachments.FirstOrDefault();

            if (profileAttachment != null)
            {
                // Fetch content
                profileAttachment = await _repository.GetByIdWithIncludesAsync<Attachment>(profileAttachment.Id, "AttachmentContent");
                if (profileAttachment?.AttachmentContent != null)
                {
                     contactDto.ProfileImageBase64 = Convert.ToBase64String(profileAttachment.AttachmentContent.Content);
                     contactDto.ProfileImageContentType = profileAttachment.ContentType;
                }
            }

            return View(contactDto);
        }

        // GET: Contacts/Create
        public IActionResult Create()
        {
            return View(new CreateContactDto());
        }

        // POST: Contacts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,Nickname,Email,Phone,JobTitle,Company,Birthday")] CreateContactDto contactDto)
        {
            if (ModelState.IsValid)
            {
                var contact = contactDto.ToEntity();
                await _repository.AddAsync(contact);
                await _repository.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(contactDto);
        }

        // GET: Contacts/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            var contact = await _repository.GetByIdAsync<Contact>(id.Value);
            if (contact == null) return NotFound();

            var dto = new UpdateContactDto
            {
                Id = contact.Id,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Nickname = contact.Nickname,
                Email = contact.Email,
                Phone = contact.Phone,
                JobTitle = contact.JobTitle,
                Company = contact.Company,
                Birthday = contact.Birthday
            };
            return View(dto);
        }

        // POST: Contacts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,FirstName,LastName,Nickname,Email,Phone,JobTitle,Company,Birthday")] UpdateContactDto contactDto, IFormFile? profileImage)
        {
            if (id != contactDto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingContact = await _repository.GetByIdAsync<Contact>(id);
                    if (existingContact == null) return NotFound();

                    existingContact.UpdateEntity(contactDto);

                    if (profileImage != null && profileImage.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        await profileImage.CopyToAsync(ms);
                        var fileBytes = ms.ToArray();

                        var existingAttachments = await _repository.ListAsync<Attachment>(a => a.EntityId == id && a.EntityType == EntityTypes.Person && a.AttachmentType == "ProfileImage");
                        var existingAttachment = existingAttachments.FirstOrDefault();

                        if (existingAttachment != null)
                        {
                            // Load content to update it
                            existingAttachment = await _repository.GetByIdWithIncludesAsync<Attachment>(existingAttachment.Id, "AttachmentContent");
                            if (existingAttachment != null)
                            {
                                if (existingAttachment.AttachmentContent == null)
                                {
                                    existingAttachment.AttachmentContent = new AttachmentContent { AttachmentId = existingAttachment.Id };
                                }
                                existingAttachment.AttachmentContent.Content = fileBytes;

                                existingAttachment.ContentType = profileImage.ContentType;
                                existingAttachment.FileName = profileImage.FileName;
                                await _repository.UpdateAsync(existingAttachment);
                            }
                        }
                        else
                        {
                            var attachment = new Attachment
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

        // GET: Contacts/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();
            var contact = await _repository.GetByIdAsync<Contact>(id.Value);
            if (contact == null) return NotFound();
            return View(contact);
        }

        // POST: Contacts/Delete/5
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
            var notes = await _repository.ListAsync<Note>(n => n.EntityId == contactId && n.EntityType == EntityTypes.Person);
            if (notes.Any()) await _repository.DeleteRangeAsync(notes);

            var reminders = await _repository.ListAsync<Reminder>(r => r.EntityId == contactId && r.EntityType == EntityTypes.Person);
            if (reminders.Any()) await _repository.DeleteRangeAsync(reminders);

            var importantDates = await _repository.ListAsync<ImportantDate>(d => d.EntityId == contactId && d.EntityType == EntityTypes.Person);
            if (importantDates.Any()) await _repository.DeleteRangeAsync(importantDates);

            var pets = await _repository.ListAsync<Pet>(p => p.EntityId == contactId && p.EntityType == EntityTypes.Person);
            if (pets.Any()) await _repository.DeleteRangeAsync(pets);

            var contactInfos = await _repository.ListAsync<ContactInfo>(i => i.EntityId == contactId && i.EntityType == EntityTypes.Person);
            if (contactInfos.Any()) await _repository.DeleteRangeAsync(contactInfos);

            var facts = await _repository.ListAsync<Fact>(f => f.EntityId == contactId && f.EntityType == EntityTypes.Person);
            if (facts.Any()) await _repository.DeleteRangeAsync(facts);

            var attachments = await _repository.ListAsync<Attachment>(a => a.EntityId == contactId && a.EntityType == EntityTypes.Person);
            if (attachments.Any()) await _repository.DeleteRangeAsync(attachments);

            // Relationships where this contact is the Source
            var relationships = await _repository.ListAsync<Relationship>(r => r.EntityId == contactId && r.EntityType == EntityTypes.Person);
            if (relationships.Any()) await _repository.DeleteRangeAsync(relationships);

            // Relationships where this contact is the Target
            var relatedTo = await _repository.ListAsync<Relationship>(r => r.RelatedEntityId == contactId && r.EntityType == EntityTypes.Person);
            if (relatedTo.Any()) await _repository.DeleteRangeAsync(relatedTo);

            // PhoneNumbers
            var phoneNumbers = await _repository.ListAsync<PhoneNumber>(p => p.EntityId == contactId && p.EntityType == EntityTypes.Person);
            if (phoneNumbers.Any()) await _repository.DeleteRangeAsync(phoneNumbers);
        }

        private async Task<bool> ContactExists(Guid id)
        {
            return await _repository.ExistsAsync<Contact>(id);
        }
    }
}
