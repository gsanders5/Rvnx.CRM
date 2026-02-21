namespace Rvnx.CRM.Core.DTOs.Common;

public class RelationshipOperationResult
{
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Guid RedirectId { get; private set; }
    public string? EntityType { get; private set; }

    public static RelationshipOperationResult Ok(Guid redirectId, string entityType) => new() { Success = true, RedirectId = redirectId, EntityType = entityType };
    public static RelationshipOperationResult Failure(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
}
