using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;

namespace Rvnx.CRM.Tests.Services;

public class RelationshipTypeServiceTests
{
    [Fact]
    public void GetAllReturnsAllTypes()
    {
        IReadOnlyList<RelationshipTypeDefinition> result = RelationshipTypeService.GetAll();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Count > 0);
    }

    [Fact]
    public void GetByEntityTypePersonReturnsOnlyPersonTypes()
    {
        List<RelationshipTypeDefinition> result = RelationshipTypeService.GetByEntityType(EntityTypes.Person);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, t => Assert.Equal(EntityTypes.Person, t.EntityType));
    }

    [Fact]
    public void GetByEntityTypeCompanyReturnsOnlyCompanyTypes()
    {
        List<RelationshipTypeDefinition> result = RelationshipTypeService.GetByEntityType(EntityTypes.Company);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, t => Assert.Equal(EntityTypes.Company, t.EntityType));
    }

    [Fact]
    public void GetByEntityTypeInvalidTypeReturnsEmptyList()
    {
        List<RelationshipTypeDefinition> result = RelationshipTypeService.GetByEntityType("InvalidType");

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetByIdValidIdReturnsCorrectType()
    {
        Guid spouseId = Guid.Parse("b2e9a5c8-7f4d-4a1b-8c6e-5f9d3a0e2b4c");

        RelationshipTypeDefinition? result = RelationshipTypeService.GetById(spouseId);

        Assert.NotNull(result);
        Assert.Equal(spouseId, result.Id);
        Assert.Equal("Spouse", result.Name);
    }

    [Fact]
    public void GetByIdInvalidIdReturnsNull()
    {
        Guid randomId = Guid.NewGuid();

        RelationshipTypeDefinition? result = RelationshipTypeService.GetById(randomId);

        Assert.Null(result);
    }
}
