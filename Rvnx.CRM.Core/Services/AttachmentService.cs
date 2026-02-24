using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Services;

public class AttachmentService : IAttachmentService
{
    private readonly IRepository _repository;
    private readonly IFileValidationService _fileValidationService;
    private readonly IEntityService _entityService;

    public AttachmentService(IRepository repository, IFileValidationService fileValidationService, IEntityService entityService)
    {
        _repository = repository;
        _fileValidationService = fileValidationService;
        _entityService = entityService;
    }

    public async Task<AttachmentOperationResult> UploadAttachmentAsync(Guid entityId, string entityType, byte[] content, string fileName)
    {
        if (string.IsNullOrEmpty(entityType))
        {
            return AttachmentOperationResult.Failure("Entity Type is required.");
        }

        // Currently, attachments are only supported for Contacts due to schema limitations
        if (!string.Equals(entityType, EntityTypes.Person, StringComparison.OrdinalIgnoreCase))
        {
            return AttachmentOperationResult.Failure($"Attachments are not currently supported for {entityType}.");
        }

        if (!await _entityService.ExistsAsync(entityType, entityId))
        {
            return AttachmentOperationResult.NotFound($"Entity not found.");
        }

        // Validate Partial Contact restriction
        if (await IsPartialContactAsync(entityId))
        {
            return AttachmentOperationResult.NotFound("Cannot add attachment to partial contact.");
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
            ContactId = entityId,
            AttachmentType = "General",
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

    public async Task<AttachmentOperationResult> DeleteAttachmentAsync(Guid attachmentId)
    {
        Attachment? attachment = await _repository.GetByIdAsync<Attachment>(attachmentId);
        if (attachment == null)
        {
            return AttachmentOperationResult.NotFound();
        }

        if (attachment.ContactId.HasValue && await IsPartialContactAsync(attachment.ContactId.Value))
        {
            return AttachmentOperationResult.NotFound("Cannot modify partial contact.");
        }

        await _repository.DeleteAsync<Attachment>(attachmentId);
        await _repository.SaveChangesAsync();

        return AttachmentOperationResult.Ok(attachmentId);
    }

    public async Task<AttachmentContentDto?> GetAttachmentContentAsync(Guid attachmentId)
    {
        Attachment? attachment = await _repository.GetByIdWithIncludesAsync<Attachment>(attachmentId, nameof(Attachment.AttachmentContent));

        return attachment?.AttachmentContent == null
            ? null
            : attachment.ContactId.HasValue && await IsPartialContactAsync(attachment.ContactId.Value)
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

    public async Task<AttachmentDto?> GetAttachmentAsync(Guid attachmentId)
    {
        Attachment? attachment = await _repository.GetByIdAsync<Attachment>(attachmentId);
        return attachment == null
            ? null
            : attachment.ContactId.HasValue && await IsPartialContactAsync(attachment.ContactId.Value)
            ? null
            : new AttachmentDto
            {
                Id = attachment.Id,
                FileName = attachment.FileName ?? string.Empty,
                ContentType = attachment.ContentType,
                AttachmentType = attachment.AttachmentType,
                EntityId = attachment.ContactId ?? Guid.Empty,
                EntityType = EntityTypes.Person
            };
    }

    private async Task<bool> IsPartialContactAsync(Guid contactId)
    {
        Contact? c = await _repository.GetByIdAsync<Contact>(contactId);
        return c?.IsPartial == true;
    }
}
