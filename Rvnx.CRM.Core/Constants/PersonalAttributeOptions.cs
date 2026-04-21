namespace Rvnx.CRM.Core.Constants;

public static class PersonalAttributeOptions
{
    public const string Male = "Male";
    public const string Female = "Female";
    public const string NonBinary = "Non-Binary";
    public const string Other = "Other";
    public const string Unspecified = "Unspecified";

    public static readonly IReadOnlyList<string> Pronouns =
    [
        "He/Him",
        "She/Her",
        "They/Them",
        Other,
        Unspecified
    ];

    public static readonly IReadOnlyList<string> Gender =
    [
        Male,
        Female,
        NonBinary,
        Other,
        Unspecified
    ];
}
