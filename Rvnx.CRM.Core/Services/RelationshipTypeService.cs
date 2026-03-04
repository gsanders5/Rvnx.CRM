using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Services;

public static class RelationshipTypeIds
{
    public static readonly Guid Spouse = Guid.Parse("b2e9a5c8-7f4d-4a1b-8c6e-5f9d3a0e2b4c");
    public static readonly Guid ExSpouse = Guid.Parse("b2e9a5c8-7f6d-4a1b-8c6e-5f9d3a0e2b4c");
    public static readonly Guid Parent = Guid.Parse("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a");
    public static readonly Guid Sibling = Guid.Parse("d4f1b8a9-3e2c-4b5d-9a6f-1c0e7d8b5a2f");
    public static readonly Guid Grandparent = Guid.Parse("b1443742-884b-4e64-aeac-7d552a02679c");
    public static readonly Guid UncleAunt = Guid.Parse("d5368632-deca-4d04-8102-cdc891fcf566");
    public static readonly Guid Cousin = Guid.Parse("f7df8278-11f1-49a0-bfc9-960e1c40f3cd");
    public static readonly Guid Godparent = Guid.Parse("5f40f3cd-a1ad-4985-ad4e-e512fcff4a1e");
    public static readonly Guid StepParent = Guid.Parse("6ad40b7f-9df5-4a73-8fe4-0bb17b6a3570");
    public static readonly Guid StepSibling = Guid.Parse("985d3d47-1585-4788-867d-3062c2b9b78a");
    public static readonly Guid HalfSibling = Guid.Parse("3553b74c-88ba-4c1b-957c-867d6d80f319");

    public static readonly Guid SignificantOther = Guid.Parse("f9e8d7c6-b5a4-3210-9876-543210fedcbb");
    public static readonly Guid ExPartner = Guid.Parse("f9e8d7c6-b5a4-3210-9876-543210fedcbe");

    public static readonly Guid Manager = Guid.Parse("1a2b3c4d-5e6f-7890-a1b2-c3d4e5f67890");
    public static readonly Guid Mentor = Guid.Parse("1a2b3c4d-5e6f-7890-a1b2-c3d4e5f67891");
    public static readonly Guid Teacher = Guid.Parse("09876543-210f-edcb-a987-6543210fedcb");
    public static readonly Guid Colleague = Guid.Parse("c2497ebc-1ec3-427b-a845-67d31f136f56");
    public static readonly Guid BusinessPartner = Guid.Parse("36a10f9d-dc1a-4803-ae52-26fd9b9c7ec5");

    public static readonly Guid Friend = Guid.Parse("a5b6c7d8-9e0f-1a2b-3c4d-5e6f7a8b9c0d");
    public static readonly Guid BestFriend = Guid.Parse("a5b6c7d8-9e0f-1a2b-3c4d-5e6f7a8b9c0e");
    public static readonly Guid Acquaintance = Guid.Parse("71186e94-7048-4b3c-a854-5482328ab505");

    public static readonly Guid ParentCompany = Guid.Parse("fedcba98-7654-3210-fedc-ba9876543210");
}

public static class RelationshipTypeService
{
    public static readonly IReadOnlySet<Guid> TransitiveRelationshipTypeIds = new HashSet<Guid>
    {
        RelationshipTypeIds.Sibling,
        RelationshipTypeIds.Cousin,
        RelationshipTypeIds.StepSibling,
        RelationshipTypeIds.HalfSibling,
        RelationshipTypeIds.Colleague,
        RelationshipTypeIds.BusinessPartner
    };

    public static readonly IReadOnlySet<Guid> FamilyAdultChildRelationshipTypeIds = new HashSet<Guid>
    {
        RelationshipTypeIds.Parent,
        RelationshipTypeIds.Grandparent,
        RelationshipTypeIds.UncleAunt,
        RelationshipTypeIds.Godparent,
        RelationshipTypeIds.StepParent
    };

    private static readonly Lazy<List<RelationshipTypeDefinition>> _types = new(() =>
    [
        new(RelationshipTypeIds.Spouse, "Spouse", "Spouse", "Family", EntityTypes.Person,
            NameMale: "Husband", NameFemale: "Wife", OppositeNameMale: "Husband", OppositeNameFemale: "Wife"),

        new(RelationshipTypeIds.ExSpouse, "Ex-Spouse", "Ex-Spouse", "Family", EntityTypes.Person,
            NameMale: "Ex-Husband", NameFemale: "Ex-Wife", OppositeNameMale: "Ex-Husband", OppositeNameFemale: "Ex-Wife"),

        new(RelationshipTypeIds.Parent, "Parent", "Child", "Family", EntityTypes.Person,
            NameMale: "Father", NameFemale: "Mother", OppositeNameMale: "Son", OppositeNameFemale: "Daughter"),

        new(RelationshipTypeIds.Sibling, "Sibling", "Sibling", "Family", EntityTypes.Person,
            NameMale: "Brother", NameFemale: "Sister", OppositeNameMale: "Brother", OppositeNameFemale: "Sister"),

        new(RelationshipTypeIds.Grandparent, "Grandparent", "Grandchild", "Family", EntityTypes.Person,
            NameMale: "Grandfather", NameFemale: "Grandmother", OppositeNameMale: "Grandson", OppositeNameFemale: "Granddaughter"),

        new(RelationshipTypeIds.UncleAunt, "Uncle/Aunt", "Nephew/Niece", "Family", EntityTypes.Person,
            NameMale: "Uncle", NameFemale: "Aunt", OppositeNameMale: "Nephew", OppositeNameFemale: "Niece"),

        new(RelationshipTypeIds.Cousin, "Cousin", "Cousin", "Family", EntityTypes.Person),

        new(RelationshipTypeIds.Godparent, "Godparent", "Godchild", "Family", EntityTypes.Person,
            NameMale: "Godfather", NameFemale: "Godmother", OppositeNameMale: "Godson", OppositeNameFemale: "Goddaughter"),

        new(RelationshipTypeIds.StepParent, "Step-Parent", "Step-Child", "Family", EntityTypes.Person,
            NameMale: "Step-Father", NameFemale: "Step-Mother", OppositeNameMale: "Step-Son", OppositeNameFemale: "Step-Daughter"),

        new(RelationshipTypeIds.StepSibling, "Step-Sibling", "Step-Sibling", "Family", EntityTypes.Person,
            NameMale: "Step-Brother", NameFemale: "Step-Sister", OppositeNameMale: "Step-Brother", OppositeNameFemale: "Step-Sister"),

        new(RelationshipTypeIds.HalfSibling, "Half-Sibling", "Half-Sibling", "Family", EntityTypes.Person,
            NameMale: "Half-Brother", NameFemale: "Half-Sister", OppositeNameMale: "Half-Brother", OppositeNameFemale: "Half-Sister"),

        new(RelationshipTypeIds.SignificantOther, "Significant Other", "Significant Other", "Romantic", EntityTypes.Person),
        new(RelationshipTypeIds.ExPartner, "Ex-Partner", "Ex-Partner", "Romantic", EntityTypes.Person),

        new(RelationshipTypeIds.Manager, "Manager", "Employee", "Professional", EntityTypes.Person),
        new(RelationshipTypeIds.Mentor, "Mentor", "Protege", "Professional", EntityTypes.Person),
        new(RelationshipTypeIds.Teacher, "Teacher", "Student", "Professional", EntityTypes.Person),
        new(RelationshipTypeIds.Colleague, "Colleague", "Colleague", "Professional", EntityTypes.Person),
        new(RelationshipTypeIds.BusinessPartner, "Business Partner", "Business Partner", "Professional", EntityTypes.Person),

        new(RelationshipTypeIds.Friend, "Friend", "Friend", "Social", EntityTypes.Person),
        new(RelationshipTypeIds.BestFriend, "Best Friend", "Best Friend", "Social", EntityTypes.Person),
        new(RelationshipTypeIds.Acquaintance, "Acquaintance", "Acquaintance", "Social", EntityTypes.Person),

        new(RelationshipTypeIds.ParentCompany, "Parent Company", "Subsidiary", "Company", EntityTypes.Company)
    ]);

    private static readonly Lazy<Dictionary<Guid, RelationshipTypeDefinition>> _byId =
        new(() => _types.Value.ToDictionary(t => t.Id));

    private static readonly Lazy<ILookup<string, RelationshipTypeDefinition>> _byEntityType =
        new(() => _types.Value.ToLookup(t => t.EntityType));

    /// <summary>
    /// Returns all available relationship types.
    /// </summary>
    public static IReadOnlyList<RelationshipTypeDefinition> GetAll()
    {
        return _types.Value;
    }

    /// <summary>
    /// Returns all relationship types valid for a specific entity type.
    /// </summary>
    public static List<RelationshipTypeDefinition> GetByEntityType(string entityType)
    {
        return [.. _byEntityType.Value[entityType]];
    }

    /// <summary>
    /// Returns a relationship type definition by its ID.
    /// </summary>
    public static RelationshipTypeDefinition? GetById(Guid id)
    {
        return _byId.Value.TryGetValue(id, out RelationshipTypeDefinition? result) ? result : null;
    }
}
