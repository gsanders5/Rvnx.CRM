using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Tests.Models;

public class RelationshipTypeDefinitionTests
{
    [Fact]
    public void GetNameWithMaleGenderResolvesToMaleNameWhenProvided()
    {
        var definition = new RelationshipTypeDefinition(Guid.NewGuid(), "Parent", "Child", "Family", EntityType.Person, NameMale: "Father", NameFemale: "Mother");

        string result = definition.GetName("Male");

        Assert.Equal("Father", result);
    }

    [Fact]
    public void GetNameWithNullGenderFallsBackToNeutralName()
    {
        var definition = new RelationshipTypeDefinition(Guid.NewGuid(), "Parent", "Child", "Family", EntityType.Person, NameMale: "Father", NameFemale: "Mother");

        string result = definition.GetName(null);

        Assert.Equal("Parent", result);
    }

    [Fact]
    public void IsSymmetricReturnsTrueWhenNameEqualsOppositeName()
    {
        var definition = new RelationshipTypeDefinition(Guid.NewGuid(), "Spouse", "Spouse", "Family", EntityType.Person);

        Assert.True(definition.IsSymmetric);
    }

    [Fact]
    public void IsSymmetricReturnsFalseWhenNameDiffersFromOppositeName()
    {
        var definition = new RelationshipTypeDefinition(Guid.NewGuid(), "Parent", "Child", "Family", EntityType.Person);

        Assert.False(definition.IsSymmetric);
    }
}
