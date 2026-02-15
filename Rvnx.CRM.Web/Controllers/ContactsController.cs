using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Constants;

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
            return View(contacts);
        }

        // GET: Contacts/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var contact = await _repository.GetByIdWithIncludesAsync<Contact>(id.Value, "Employers");
            if (contact == null) return NotFound();

            // Manually fetch related entities
            contact.Notes = await _repository.ListAsync<Note>(n => n.EntityId == id.Value && n.EntityType == EntityTypes.Person);
            contact.Reminders = await _repository.ListAsync<Reminder>(r => r.EntityId == id.Value && r.EntityType == EntityTypes.Person);
            contact.ImportantDates = await _repository.ListAsync<ImportantDate>(d => d.EntityId == id.Value && d.EntityType == EntityTypes.Person);

            var types = await _repository.ListAsync<RelationshipType>();

            // Relationships
            var relationships = await _repository.ListAsync<Relationship>(r => r.EntityId == id.Value && r.EntityType == EntityTypes.Person);
            foreach (var rel in relationships)
            {
                rel.RelationshipType = types.FirstOrDefault(t => t.Id == rel.RelationshipTypeId);
                rel.RelatedPerson = await _repository.GetByIdAsync<Contact>(rel.RelatedEntityId); // Assuming Contacts for now
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

            // Profile Image
            var profileAttachments = await _repository.ListAsync<Attachment>(a => a.EntityId == id.Value && a.EntityType == EntityTypes.Person && a.AttachmentType == "ProfileImage");
            var profileAttachment = profileAttachments.FirstOrDefault();

            if (profileAttachment != null)
            {
                // Fetch content
                profileAttachment = await _repository.GetByIdWithIncludesAsync<Attachment>(profileAttachment.Id, "AttachmentContent");
                if (profileAttachment?.AttachmentContent != null)
                {
                     ViewBag.ProfileImageBase64 = Convert.ToBase64String(profileAttachment.AttachmentContent.Content);
                     ViewBag.ProfileImageContentType = profileAttachment.ContentType;
                }
            }

            return View(contact);
        }

        // GET: Contacts/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Contacts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,Nickname,Email,Phone,JobTitle,Company,Birthday,UserId")] Contact contact)
        {
            if (ModelState.IsValid)
            {
                contact.Id = Guid.NewGuid();
                await _repository.AddAsync(contact);
                await _repository.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(contact);
        }

        // GET: Contacts/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            var contact = await _repository.GetByIdAsync<Contact>(id.Value);
            if (contact == null) return NotFound();
            return View(contact);
        }

        // POST: Contacts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,FirstName,LastName,Nickname,Email,Phone,JobTitle,Company,Birthday")] Contact contact, IFormFile? profileImage)
        {
            if (id != contact.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingContact = await _repository.GetByIdAsync<Contact>(id);
                    if (existingContact == null) return NotFound();

                    existingContact.FirstName = contact.FirstName;
                    existingContact.LastName = contact.LastName;
                    existingContact.Nickname = contact.Nickname;
                    existingContact.Email = contact.Email;
                    existingContact.Phone = contact.Phone;
                    existingContact.JobTitle = contact.JobTitle;
                    existingContact.Company = contact.Company;
                    existingContact.Birthday = contact.Birthday;

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
                            if (existingAttachment.AttachmentContent == null)
                            {
                                existingAttachment.AttachmentContent = new AttachmentContent { AttachmentId = existingAttachment.Id };
                            }
                            existingAttachment.AttachmentContent.Content = fileBytes;

                            existingAttachment.ContentType = profileImage.ContentType;
                            existingAttachment.FileName = profileImage.FileName;
                            await _repository.UpdateAsync(existingAttachment);
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
                    if (!await ContactExists(contact.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(contact);
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
            // Fetch related entities manually
            var relationships = await _repository.ListAsync<Relationship>(r => r.EntityId == id && r.EntityType == EntityTypes.Person);
            if (relationships.Any()) await _repository.DeleteRangeAsync(relationships);

            var relatedTo = await _repository.ListAsync<Relationship>(r => r.RelatedEntityId == id && r.EntityType == EntityTypes.Person);
            if (relatedTo.Any()) await _repository.DeleteRangeAsync(relatedTo);

            var notes = await _repository.ListAsync<Note>(n => n.EntityId == id && n.EntityType == EntityTypes.Person);
            if (notes.Any()) await _repository.DeleteRangeAsync(notes);

            var reminders = await _repository.ListAsync<Reminder>(r => r.EntityId == id && r.EntityType == EntityTypes.Person);
            if (reminders.Any()) await _repository.DeleteRangeAsync(reminders);

            var importantDates = await _repository.ListAsync<ImportantDate>(d => d.EntityId == id && d.EntityType == EntityTypes.Person);
            if (importantDates.Any()) await _repository.DeleteRangeAsync(importantDates);

            // Attachments
             var attachments = await _repository.ListAsync<Attachment>(a => a.EntityId == id && a.EntityType == EntityTypes.Person);
             if (attachments.Any())
             {
                 // Content deletes via Cascade?
                 // AttachmentContent has FK to Attachment with Cascade Delete in OnModelCreating.
                 // So deleting Attachment should delete Content.
                 await _repository.DeleteRangeAsync(attachments);
             }

            // Finally delete contact
            await _repository.DeleteAsync<Contact>(id);
            await _repository.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ContactExists(Guid id)
        {
            return await _repository.ExistsAsync<Contact>(id);
        }
    }
}
