namespace Rvnx.CRM.Core.DTOs.DebugOperations;

public class MergeUserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int GroupMemberCount { get; set; }
}