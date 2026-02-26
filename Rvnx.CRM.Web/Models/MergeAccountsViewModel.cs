namespace Rvnx.CRM.Web.Models;

public class MergeAccountsViewModel
{
    public List<MergeUserDto> Users { get; set; } = [];
}

public class MergeUserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int GroupMemberCount { get; set; }
}
