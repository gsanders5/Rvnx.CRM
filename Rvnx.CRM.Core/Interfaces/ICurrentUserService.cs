namespace Rvnx.CRM.Core.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
}
