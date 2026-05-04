namespace Rvnx.CRM.Core.Models.Contact;

public record RelationshipTypeDefinition(Guid Id, string Name, string OppositeName, string Category,
    string? NameMale = null, string? NameFemale = null,
    string? OppositeNameMale = null, string? OppositeNameFemale = null)
{
    public bool IsSymmetric => Name == OppositeName;

    public string GetName(string? gender) => ResolveByGender(gender, Name, NameMale, NameFemale);

    public string GetOppositeName(string? gender) => ResolveByGender(gender, OppositeName, OppositeNameMale, OppositeNameFemale);

    private static string ResolveByGender(string? gender, string neutral, string? male, string? female)
    {
        if (string.Equals(gender, "Male", StringComparison.OrdinalIgnoreCase))
        {
            return male ?? neutral;
        }
        if (string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase))
        {
            return female ?? neutral;
        }
        return neutral;
    }
}
