using Microsoft.Extensions.Logging;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Infrastructure.Services;

public class ContactImportService(IRepository repository, IVCardService vCardService, ILogger<ContactImportService> logger) : IContactImportService
{
    private readonly IRepository _repository = repository;
    private readonly IVCardService _vCardService = vCardService;
    private readonly ILogger<ContactImportService> _logger = logger;

    public async Task<ContactImportResult> ImportFromVCardAsync(Stream vCardStream)
    {
        try
        {
            IEnumerable<Contact> importedContacts = _vCardService.ParseVCard(vCardStream);

            int addedCount = 0;
            int skippedCount = 0;

            foreach (Contact contact in importedContacts)
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
                addedCount++;
            }

            return new ContactImportResult
            {
                AddedCount = addedCount,
                SkippedCount = skippedCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing VCF");
            throw;
        }
    }

    private async Task<bool> IsDuplicateAsync(Contact candidate)
    {
        if (await _repository.CountAsync<Contact>(c => c.FirstName == candidate.FirstName && c.LastName == candidate.LastName) > 0)
        {
            return true;
        }

        if (candidate.ContactMethods?.Any() == true)
        {
            List<string> valuesToCheck = candidate.ContactMethods.Select(m => m.Value).ToList();
            return await _repository.CountAsync<ContactMethod>(cm =>
                valuesToCheck.Contains(cm.Value)) > 0;
        }

        return false;
    }
}
