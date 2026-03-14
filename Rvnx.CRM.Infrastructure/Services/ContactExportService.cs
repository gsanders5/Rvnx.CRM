using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Infrastructure.Services;

public class ContactExportService(IRepository repository, IVCardService vCardService) : IContactExportService
{
    private readonly IRepository _repository = repository;
    private readonly IVCardService _vCardService = vCardService;

    public async Task<ContactExportResult> ExportToVCardAsync(Guid contactId)
    {
        Contact? contact = await _repository.GetByIdAsync<Contact>(contactId);
        if (contact == null)
        {
            throw new KeyNotFoundException($"Contact with ID {contactId} not found.");
        }

        contact.ContactMethods = await _repository.ListAsNoTrackingAsync<ContactMethod>(e => e.ContactId == contactId);
        contact.SignificantDates = await _repository.ListAsNoTrackingAsync<SignificantDate>(e => e.ContactId == contactId);
        contact.Attachments = await _repository.ListAsNoTrackingAsync<Attachment>(
            a => a.ContactId == contactId && a.AttachmentType == AttachmentTypes.ProfileImage,
            default,
            nameof(Attachment.AttachmentContent));

        byte[] vcfBytes = _vCardService.ExportVCard(contact);
        string fileName = $"{contact.FirstName}_{contact.LastName}.vcf";

        return new ContactExportResult
        {
            FileContent = vcfBytes,
            FileName = fileName,
            ContentType = "text/vcard"
        };
    }
}