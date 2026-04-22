using Rvnx.CRM.Core.DTOs.Base;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class LabelOperationResult : EntityOperationResult
{
    public Guid? LabelId { get; set; }

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
