namespace Rvnx.CRM.Core.Enumerations;

public static class CoreEnumerations
{
    public enum PhoneNumberType
    {
        Unknown = 0,
        Mobile = 1,
        Home = 2,
        Work = 3,
        Other = 4
    }

    /// <summary>
    /// Direction of a relationship when the type is asymmetric.
    /// For symmetric types (Friend, Sibling, Colleague) both directions are equivalent.
    /// </summary>
    public enum RelationshipDirection
    {
        /// <summary>Applies the type's forward name (e.g. "Parent").</summary>
        Forward = 0,
        /// <summary>Applies the type's opposite name (e.g. "Child").</summary>
        Reverse = 1
    }
}