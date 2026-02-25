using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Services;

public static class RelationshipTypeService
{
    private static readonly List<RelationshipTypeDefinition> _types =
    [
        // Family
        new(Guid.Parse("b2e9a5c8-7f4d-4a1b-8c6e-5f9d3a0e2b4c"), "Spouse", "Spouse", "Family", EntityTypes.Person,
            NameMale: "Husband", NameFemale: "Wife", OppositeNameMale: "Husband", OppositeNameFemale: "Wife"),

        new(Guid.Parse("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a"), "Parent", "Child", "Family", EntityTypes.Person,
            NameMale: "Father", NameFemale: "Mother", OppositeNameMale: "Son", OppositeNameFemale: "Daughter"),

        new(Guid.Parse("d4f1b8a9-3e2c-4b5d-9a6f-1c0e7d8b5a2f"), "Sibling", "Sibling", "Family", EntityTypes.Person,
            NameMale: "Brother", NameFemale: "Sister", OppositeNameMale: "Brother", OppositeNameFemale: "Sister"),

        new(Guid.Parse("11111111-1111-1111-1111-111111111107"), "Grandparent", "Grandchild", "Family", EntityTypes.Person,
            NameMale: "Grandfather", NameFemale: "Grandmother", OppositeNameMale: "Grandson", OppositeNameFemale: "Granddaughter"),

        new(Guid.Parse("11111111-1111-1111-1111-111111111108"), "Uncle/Aunt", "Nephew/Niece", "Family", EntityTypes.Person,
            NameMale: "Uncle", NameFemale: "Aunt", OppositeNameMale: "Nephew", OppositeNameFemale: "Niece"),

        new(Guid.Parse("11111111-1111-1111-1111-111111111109"), "Cousin", "Cousin", "Family", EntityTypes.Person),

        new(Guid.Parse("11111111-1111-1111-1111-111111111110"), "Godparent", "Godchild", "Family", EntityTypes.Person,
            NameMale: "Godfather", NameFemale: "Godmother", OppositeNameMale: "Godson", OppositeNameFemale: "Goddaughter"),

        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Step-Parent", "Step-Child", "Family", EntityTypes.Person,
            NameMale: "Step-Father", NameFemale: "Step-Mother", OppositeNameMale: "Step-Son", OppositeNameFemale: "Step-Daughter"),

        // Romantic
        new(Guid.Parse("f9e8d7c6-b5a4-3210-9876-543210fedcba"), "Partner", "Partner", "Romantic", EntityTypes.Person),
        new(Guid.Parse("f9e8d7c6-b5a4-3210-9876-543210fedcbb"), "Significant Other", "Significant Other", "Romantic", EntityTypes.Person),
        new(Guid.Parse("f9e8d7c6-b5a4-3210-9876-543210fedcbc"), "Date", "Date", "Romantic", EntityTypes.Person),
        new(Guid.Parse("f9e8d7c6-b5a4-3210-9876-543210fedcbd"), "Lover", "Lover", "Romantic", EntityTypes.Person),
        new(Guid.Parse("f9e8d7c6-b5a4-3210-9876-543210fedcbe"), "Ex-Partner", "Ex-Partner", "Romantic", EntityTypes.Person),

        // Professional
        new(Guid.Parse("1a2b3c4d-5e6f-7890-a1b2-c3d4e5f67890"), "Manager", "Employee", "Professional", EntityTypes.Person),
        new(Guid.Parse("1a2b3c4d-5e6f-7890-a1b2-c3d4e5f67891"), "Mentor", "Protege", "Professional", EntityTypes.Person),
        new(Guid.Parse("09876543-210f-edcb-a987-6543210fedcb"), "Teacher", "Student", "Professional", EntityTypes.Person),
        new(Guid.Parse("33333333-3333-3333-3333-333333333301"), "Colleague", "Colleague", "Professional", EntityTypes.Person),
        new(Guid.Parse("33333333-3333-3333-3333-333333333302"), "Business Partner", "Business Partner", "Professional", EntityTypes.Person),

        // Social
        new(Guid.Parse("a5b6c7d8-9e0f-1a2b-3c4d-5e6f7a8b9c0d"), "Friend", "Friend", "Social", EntityTypes.Person),
        new(Guid.Parse("a5b6c7d8-9e0f-1a2b-3c4d-5e6f7a8b9c0e"), "Best Friend", "Best Friend", "Social", EntityTypes.Person),
        new(Guid.Parse("44444444-4444-4444-4444-444444444401"), "Acquaintance", "Acquaintance", "Social", EntityTypes.Person),

        // Company
        new(Guid.Parse("fedcba98-7654-3210-fedc-ba9876543210"), "Parent Company", "Subsidiary", "Company", EntityTypes.Company)
    ];

    public static List<RelationshipTypeDefinition> GetAll()
    {
        return _types;
    }

    public static List<RelationshipTypeDefinition> GetByEntityType(string entityType)
    {
        return _types.Where(t => t.EntityType == entityType).ToList();
    }

    public static RelationshipTypeDefinition? GetById(Guid id)
    {
        return _types.FirstOrDefault(t => t.Id == id);
    }
}
