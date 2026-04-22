using Rvnx.CRM.Core.DTOs.Base;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class ContactOperationResult : EntityOperationResult
{
    public Guid? ContactId { get; set; }

    public static ContactOperationResult Failure(params string[] errors)
    {
        return new ContactOperationResult { Success = false, Errors = errors.ToList() };
    }

    public static ContactOperationResult Ok(Guid contactId)
    {
        return new ContactOperationResult { Success = true, ContactId = contactId };
    }

    public static ContactOperationResult NotFound(string error = "Contact not found.")
    {
        return new ContactOperationResult { Success = false, IsNotFound = true, Errors = { error } };
    }
}
