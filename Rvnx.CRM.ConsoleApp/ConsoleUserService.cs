using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.ConsoleApp;

internal sealed class ConsoleUserService : ICurrentUserService
{
    public Guid? UserId => null;

    public Guid? GroupId => null;

    public string? UserName => "System";

    public bool IsAuthenticated => false;

    public Task<bool> IsAdministratorAsync(Guid userId)
    {
        return Task.FromResult(false);
    }
}