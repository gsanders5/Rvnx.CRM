namespace Rvnx.CRM.Core.Models;

public class OperationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid RedirectId { get; set; }
    public string? RedirectType { get; set; }

    public static OperationResult Ok(Guid redirectId, string? redirectType)
    {
        return new OperationResult { Success = true, RedirectId = redirectId, RedirectType = redirectType };
    }

    public static OperationResult Failure(string errorMessage)
    {
        return new OperationResult { Success = false, ErrorMessage = errorMessage };
    }
}
