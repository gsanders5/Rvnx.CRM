using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using System.IO.Compression;
using System.Linq.Expressions;

namespace Rvnx.CRM.Infrastructure.Services;

public class ContactExportService(IRepository repository, IVCardService vCardService) : IContactExportService
{
    private static readonly HashSet<char> InvalidFileNameChars = [.. Path.GetInvalidFileNameChars()];

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

    public Task<ContactExportResult> ExportAllToVCardZipAsync(CancellationToken cancellationToken = default) =>
        BuildVCardZipAsync(ids: null, cancellationToken);

    public Task<ContactExportResult> ExportSelectedToVCardZipAsync(
        IReadOnlyCollection<Guid> contactIds,
        CancellationToken cancellationToken = default) =>
        BuildVCardZipAsync(contactIds, cancellationToken);

    private async Task<ContactExportResult> BuildVCardZipAsync(
        IReadOnlyCollection<Guid>? ids,
        CancellationToken cancellationToken)
    {
        List<Contact> contacts;
        if (ids == null || ids.Count == 0)
        {
            contacts = await _repository.ListAsNoTrackingAsync<Contact>(c => !c.IsPartial, cancellationToken);
        }
        else
        {
            HashSet<Guid> idSet = [.. ids];
            contacts = await _repository.ListByChunkedContainsAsync<Contact, Guid>(
                [.. idSet],
                chunk => c => !c.IsPartial && chunk.Contains(c.Id),
                asNoTracking: true,
                cancellationToken);
        }

        List<Guid> contactIds = [.. contacts.Select(c => c.Id)];

        Dictionary<Guid, List<ContactMethod>> methodsByContact = await LoadGroupedAsync<ContactMethod>(
            contactIds,
            chunk => cm => cm.ContactId.HasValue && chunk.Contains(cm.ContactId.Value),
            cm => cm.ContactId!.Value,
            cancellationToken);

        Dictionary<Guid, List<SignificantDate>> datesByContact = await LoadGroupedAsync<SignificantDate>(
            contactIds,
            chunk => sd => sd.ContactId.HasValue && chunk.Contains(sd.ContactId.Value),
            sd => sd.ContactId!.Value,
            cancellationToken);

        Dictionary<Guid, List<Attachment>> attachmentsByContact = await LoadGroupedAsync<Attachment>(
            contactIds,
            chunk => a => a.ContactId.HasValue && chunk.Contains(a.ContactId.Value)
                && a.AttachmentType == AttachmentTypes.ProfileImage,
            a => a.ContactId!.Value,
            cancellationToken,
            nameof(Attachment.AttachmentContent));

        using MemoryStream stream = new();
        using (ZipArchive archive = new(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            HashSet<string> usedNames = new(StringComparer.OrdinalIgnoreCase);

            foreach (Contact contact in contacts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                contact.ContactMethods = methodsByContact.TryGetValue(contact.Id, out List<ContactMethod>? methods)
                    ? methods : [];
                contact.SignificantDates = datesByContact.TryGetValue(contact.Id, out List<SignificantDate>? dates)
                    ? dates : [];
                contact.Attachments = attachmentsByContact.TryGetValue(contact.Id, out List<Attachment>? attachments)
                    ? attachments : [];

                byte[] vcfBytes = _vCardService.ExportVCard(contact);
                string entryName = BuildUniqueEntryName(contact, usedNames);

                ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                await using Stream entryStream = entry.Open();
                await entryStream.WriteAsync(vcfBytes, cancellationToken);
            }
        }

        return new ContactExportResult
        {
            FileContent = stream.ToArray(),
            FileName = $"contacts_vcard_{DateTime.Now:yyyyMMdd_HHmmss}.zip",
            ContentType = "application/zip"
        };
    }

    private async Task<Dictionary<Guid, List<T>>> LoadGroupedAsync<T>(
        List<Guid> contactIds,
        Func<IEnumerable<Guid>, Expression<Func<T, bool>>> predicateBuilder,
        Func<T, Guid> keySelector,
        CancellationToken cancellationToken,
        params string[] includes) where T : BaseEntity
    {
        if (contactIds.Count == 0)
        {
            return [];
        }

        List<T> rows = await _repository.ListByChunkedContainsAsync(
            contactIds, predicateBuilder, asNoTracking: true, cancellationToken, includes);

        // Optimization: Pre-allocate Dictionary and use foreach loop instead of GroupBy().ToDictionary(...) to avoid IGrouping allocations
        Dictionary<Guid, List<T>> result = [];
        foreach (T row in rows)
        {
            Guid key = keySelector(row);
            if (!result.TryGetValue(key, out List<T>? list))
            {
                list = [];
                result[key] = list;
            }
            list.Add(row);
        }

        return result;
    }

    private static string BuildUniqueEntryName(Contact contact, HashSet<string> usedNames)
    {
        string baseName = SanitizeFileName($"{contact.FirstName}_{contact.LastName}".Trim('_'));
        if (string.IsNullOrEmpty(baseName))
        {
            baseName = $"contact-{contact.Id}";
        }

        string candidate = $"{baseName}.vcf";
        int counter = 2;
        while (!usedNames.Add(candidate))
        {
            candidate = $"{baseName}_{counter}.vcf";
            counter++;
        }
        return candidate;
    }

    private static string SanitizeFileName(string name)
    {
        return string.Concat(name.Where(c => !InvalidFileNameChars.Contains(c)));
    }
}
