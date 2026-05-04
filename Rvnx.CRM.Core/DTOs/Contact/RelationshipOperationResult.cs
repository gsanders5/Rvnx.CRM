namespace Rvnx.CRM.Core.DTOs.Contact;

public class RelationshipOperationResult
{
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Guid RedirectId { get; private set; }

    public static RelationshipOperationResult Ok(Guid redirectId)
    {
        return new() { Success = true, RedirectId = redirectId };
    }

    public static RelationshipOperationResult Failure(string errorMessage)
    {
        return new() { Success = false, ErrorMessage = errorMessage };
    }
}
