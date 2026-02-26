namespace Rvnx.CRM.Core.Constants
{
    public static class PersonalAttributeOptions
    {
        public const string Unspecified = "Unspecified";

        public static readonly IReadOnlyList<string> Pronouns =
        [
            "He/Him",
            "She/Her",
            "They/Them",
            "Other",
            Unspecified
        ];

        public static readonly IReadOnlyList<string> Gender =
        [
            "Male",
            "Female",
            "Non-Binary",
            "Other",
            Unspecified
        ];
    }
}
