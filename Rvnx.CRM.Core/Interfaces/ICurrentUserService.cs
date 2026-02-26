namespace Rvnx.CRM.Core.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? GroupId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
}
