namespace Rvnx.CRM.Core.DTOs.Contact;

public class LabelOperationResult
{
    public bool Success { get; set; }
    public Guid? LabelId { get; set; }
    public List<string> Errors { get; set; } = [];
    public bool IsNotFound { get; set; }

    public static LabelOperationResult Failure(params string[] errors)
    {
        return new LabelOperationResult { Success = false, Errors = errors.ToList() };
    }

    public static LabelOperationResult Ok(Guid labelId)
    {
        return new LabelOperationResult { Success = true, LabelId = labelId };
    }

    public static LabelOperationResult NotFound(string error = "Label not found.")
    {
        return new LabelOperationResult { Success = false, IsNotFound = true, Errors = { error } };
    }
}