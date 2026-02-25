namespace Rvnx.CRM.Core.Models.Contact;

public record RelationshipTypeDefinition(Guid Id, string Name, string OppositeName, string Category, string EntityType,
    string? NameMale = null, string? NameFemale = null,
    string? OppositeNameMale = null, string? OppositeNameFemale = null)
{
    public bool IsSymmetric => Name == OppositeName;

    public string GetName(string? gender)
    {
        if (string.Equals(gender, "Male", StringComparison.OrdinalIgnoreCase)) return NameMale ?? Name;
        if (string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase)) return NameFemale ?? Name;
        return Name;
    }

    public string GetOppositeName(string? gender)
    {
        if (string.Equals(gender, "Male", StringComparison.OrdinalIgnoreCase)) return OppositeNameMale ?? OppositeName;
        if (string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase)) return OppositeNameFemale ?? OppositeName;
        return OppositeName;
    }
}
