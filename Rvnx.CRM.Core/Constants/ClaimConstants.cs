namespace Rvnx.CRM.Core.Constants;

public static class ClaimConstants
{
    /// <summary>
    /// Custom claim type for the internal CRM user ID.
    /// Using a separate claim type avoids side effects of modifying NameIdentifier,
    /// which other middleware or logging systems may have already read.
    /// </summary>
    public const string InternalUserIdClaimType = "urn:crm:internal-user-id";
    public const string InternalGroupIdClaimType = "urn:crm:internal-group-id";
}