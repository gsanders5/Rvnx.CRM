using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;

namespace Rvnx.CRM.Tests.Services;

public class RelationshipTypeServiceTests
{
    [Fact]
    public void GetAllReturnsAllTypes()
    {
        // Act
        var result = RelationshipTypeService.GetAll();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Count > 0);
    }

    [Fact]
    public void GetByEntityTypePersonReturnsOnlyPersonTypes()
    {
        // Act
        var result = RelationshipTypeService.GetByEntityType(EntityTypes.Person);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, t => Assert.Equal(EntityTypes.Person, t.EntityType));
    }

    [Fact]
    public void GetByEntityTypeCompanyReturnsOnlyCompanyTypes()
    {
        // Act
        var result = RelationshipTypeService.GetByEntityType(EntityTypes.Company);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, t => Assert.Equal(EntityTypes.Company, t.EntityType));
    }

    [Fact]
    public void GetByEntityTypeInvalidTypeReturnsEmptyList()
    {
        // Act
        var result = RelationshipTypeService.GetByEntityType("InvalidType");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetByIdValidIdReturnsCorrectType()
    {
        // Arrange
        var spouseId = Guid.Parse("b2e9a5c8-7f4d-4a1b-8c6e-5f9d3a0e2b4c");

        // Act
        var result = RelationshipTypeService.GetById(spouseId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(spouseId, result.Id);
        Assert.Equal("Spouse", result.Name);
    }

    [Fact]
    public void GetByIdInvalidIdReturnsNull()
    {
        // Arrange
        var randomId = Guid.NewGuid();

        // Act
        var result = RelationshipTypeService.GetById(randomId);

        // Assert
        Assert.Null(result);
    }
}
