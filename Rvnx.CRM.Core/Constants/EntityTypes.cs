namespace Rvnx.CRM.Core.Constants;

public static class EntityTypes
{
    public const string Person = "Person";
    public const string Company = "Company";
    public const string Opportunity = "Opportunity";

    public const string Note = "Note";
    public const string Reminder = "Reminder";
    public const string SignificantDate = "SignificantDate";
    public const string Relationship = "Relationship";
    public const string Attachment = "Attachment";

    public static readonly string[] All = {
        Person,
        Company,
        Opportunity,
        Note,
        Reminder,
        SignificantDate,
        Relationship,
        Attachment
    };

    public static bool IsValid(string type)
    {
        return All.Contains(type);
    }
}
