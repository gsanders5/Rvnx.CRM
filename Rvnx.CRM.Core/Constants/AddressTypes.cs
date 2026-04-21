namespace Rvnx.CRM.Core.Constants;

public static class AddressTypes
{
    public const string Home = "Home";
    public const string Work = "Work";
    public const string Other = "Other";

    public static readonly IReadOnlyList<string> All = [Home, Work, Other];
}
