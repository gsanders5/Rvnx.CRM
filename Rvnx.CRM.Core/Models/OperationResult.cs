using Rvnx.CRM.Core.Enumerations;

namespace Rvnx.CRM.Core.Models;

public class OperationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsNotFound { get; set; }
    public bool IsConflict { get; set; }
    public Guid RedirectId { get; set; }
    public EntityType? RedirectType { get; set; }

    public static OperationResult Ok(Guid redirectId, EntityType? redirectType)
    {
        return new OperationResult { Success = true, RedirectId = redirectId, RedirectType = redirectType };
    }

    public static OperationResult Failure(string errorMessage)
    {
        return new OperationResult { Success = false, ErrorMessage = errorMessage };
    }

    public static OperationResult NotFound(string errorMessage)
    {
        return new OperationResult { Success = false, ErrorMessage = errorMessage, IsNotFound = true };
    }

    public static OperationResult Conflict(string errorMessage)
    {
        return new OperationResult { Success = false, ErrorMessage = errorMessage, IsConflict = true };
    }
}
