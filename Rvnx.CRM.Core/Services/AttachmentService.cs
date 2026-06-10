using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Services;

public class AttachmentService : IAttachmentService
{
    private readonly IRepository _repository;
    private readonly IFileValidationService _fileValidationService;
    private readonly IContactLookupService _contactLookupService;

    public AttachmentService(IRepository repository, IFileValidationService fileValidationService, IContactLookupService contactLookupService)
    {
        _repository = repository;
        _fileValidationService = fileValidationService;
        _contactLookupService = contactLookupService;
    }

    public async Task<List<AttachmentDto>> GetByContactAsync(Guid contactId)
    {
        List<Attachment> attachments = await _repository.ListAsync<Attachment>(
            a => a.ContactId == contactId
        );
        return [.. attachments.Select(a => a.ToDto())];
    }

    /// <inheritdoc />
    public async Task<AttachmentOperationResult> UploadAttachmentAsync(Guid contactId, byte[] content, string fileName)
    {
        if (!await _repository.IsValidContactAsync(contactId))
        {
            return AttachmentOperationResult.NotFound("Contact not found or is partial.");
        }

        if (content == null || content.Length == 0)
        {
            return AttachmentOperationResult.Failure("File is empty.");
        }

        if (!_fileValidationService.IsAllowedFileSize(content.LongLength))
        {
            return AttachmentOperationResult.Failure("File is too large.");
        }

        string extension = Path.GetExtension(fileName);
        if (!_fileValidationService.IsAllowedExtension(extension))
        {
            return AttachmentOperationResult.Failure("File type not allowed.");
        }

        if (!_fileValidationService.IsValidFileSignature(content, extension))
        {
            return AttachmentOperationResult.Failure("Invalid file signature.");
        }

        string safeContentType = _fileValidationService.GetMimeType(extension);

        Attachment attachment = new()
        {
            Id = Guid.NewGuid(),
            ContactId = contactId,
            AttachmentType = AttachmentTypes.General,
            ContentType = safeContentType,
            FileName = fileName,
            AttachmentContent = new AttachmentContent
            {
                Content = content
            },
            CreatedBy = "System", // Will be overwritten by Context if tracked, but set explicitly for now
            LastChangedBy = "System",
            CreatedDate = DateTime.UtcNow,
            LastChangedDate = DateTime.UtcNow
        };

        await _repository.AddAsync(attachment);
        await _repository.SaveChangesAsync();

        return AttachmentOperationResult.Ok(attachment.Id);
    }

    /// <inheritdoc />
    public async Task<AttachmentOperationResult> DeleteAttachmentAsync(Guid attachmentId)
    {
        Attachment? attachment = await _repository.GetByIdAsync<Attachment>(attachmentId);
        if (attachment == null)
        {
            return AttachmentOperationResult.NotFound();
        }

        // GetByIdAsync resolves by primary key via FindAsync, which bypasses the global group query
        // filter, so an attachment from another group is loaded here. IsValidContactAsync runs a
        // group-filtered check (exists, not partial), rejecting cross-group and partial-owned
        // attachments alike before any deletion occurs.
        if (attachment.ContactId.HasValue && !await _repository.IsValidContactAsync(attachment.ContactId.Value))
        {
            return AttachmentOperationResult.NotFound();
        }

        await _repository.DeleteAsync<Attachment>(attachmentId);
        await _repository.SaveChangesAsync();

        return AttachmentOperationResult.Ok(attachmentId);
    }

    /// <inheritdoc />
    public async Task<AttachmentContentDto?> GetAttachmentContentAsync(Guid attachmentId)
    {
        Attachment? attachment = await _repository.GetByIdWithIncludesAsync<Attachment>(attachmentId, nameof(Attachment.AttachmentContent));

        return attachment?.AttachmentContent == null
            ? null
            : attachment.ContactId.HasValue && !await _repository.IsValidContactAsync(attachment.ContactId.Value)
            ? null
            : new AttachmentContentDto
            {
                Id = attachment.Id,
                Content = attachment.AttachmentContent.Content,
                ContentType = attachment.ContentType,
                FileName = attachment.FileName ?? "unknown",
                LastChangedDate = attachment.LastChangedDate
            };
    }

    /// <inheritdoc />
    public async Task<AttachmentDto?> GetAttachmentAsync(Guid attachmentId)
    {
        Attachment? attachment = await _repository.GetByIdAsync<Attachment>(attachmentId);
        return attachment == null
            ? null
            : attachment.ContactId.HasValue && !await _repository.IsValidContactAsync(attachment.ContactId.Value)
            ? null
            : new AttachmentDto
            {
                Id = attachment.Id,
                FileName = attachment.FileName ?? string.Empty,
                ContentType = attachment.ContentType,
                AttachmentType = attachment.AttachmentType,
                ContactId = attachment.ContactId ?? Guid.Empty
            };
    }

}
