namespace Rvnx.CRM.Core.DTOs.Base;

public class MergeOperationResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = [];
    public string KeptGroupName { get; set; } = string.Empty;
    public int KeptGroupMemberCount { get; set; }

    public static MergeOperationResult Failure(params string[] errors)
    {
        return new MergeOperationResult { Success = false, Errors = errors.ToList() };
    }

    public static MergeOperationResult Ok(string keptGroupName, int keptGroupMemberCount)
    {
        return new MergeOperationResult { Success = true, KeptGroupName = keptGroupName, KeptGroupMemberCount = keptGroupMemberCount };
    }
}
