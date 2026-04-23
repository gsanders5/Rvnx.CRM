using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Models;

namespace Rvnx.CRM.Tests.Models;

public class OperationResultTests
{
    [Fact]
    public void OkMethodSetsSuccessTrueAndRedirectProperties()
    {
        var guid = Guid.NewGuid();

        var result = OperationResult.Ok(guid, EntityType.Person);

        Assert.True(result.Success);
        Assert.Equal(guid, result.RedirectId);
        Assert.Equal(EntityType.Person, result.RedirectType);
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
