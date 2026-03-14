namespace Rvnx.CRM.Core.Models.Contact;

public record RelationshipTypeDefinition(Guid Id, string Name, string OppositeName, string Category, string EntityType,
    string? NameMale = null, string? NameFemale = null,
    string? OppositeNameMale = null, string? OppositeNameFemale = null)
{
    public bool IsSymmetric => Name == OppositeName;

    public string GetName(string? gender)
    {
        return string.Equals(gender, "Male", StringComparison.OrdinalIgnoreCase)
            ? NameMale ?? Name
            : string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase) ? NameFemale ?? Name : Name;
    }

    public string GetOppositeName(string? gender)
    {
        return string.Equals(gender, "Male", StringComparison.OrdinalIgnoreCase)
            ? OppositeNameMale ?? OppositeName
            : string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase) ? OppositeNameFemale ?? OppositeName : OppositeName;
    }
}