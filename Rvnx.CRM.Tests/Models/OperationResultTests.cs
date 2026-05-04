using Rvnx.CRM.Core.Models;

namespace Rvnx.CRM.Tests.Models;

public class OperationResultTests
{
    [Fact]
    public void OkMethodSetsSuccessTrueAndRedirectProperties()
    {
        var guid = Guid.NewGuid();

        var result = OperationResult.Ok(guid);

        Assert.True(result.Success);
        Assert.Equal(guid, result.RedirectId);
    }

    [Fact]
    public void ConflictMethodSetsPropertiesCorrectly()
    {
        var result = OperationResult.Conflict("A birthday already exists.");

        Assert.False(result.Success);
        Assert.True(result.IsConflict);
        Assert.Contains("birthday", result.ErrorMessage);
    }
}
