namespace Rvnx.CRM.Infrastructure.Services;

/// <summary>
/// Cache keys for Immich lookups, partitioned by group so groups pointing at different
/// Immich servers never see each other's people/tags. Shared between <see cref="ImmichService"/>
/// (writer) and <see cref="ImmichSettingsService"/> (invalidates on settings changes).
/// </summary>
internal static class ImmichCacheKeys
{
    public static string People(Guid? groupId) => $"immich:{groupId?.ToString() ?? "local"}:people:all";

    public static string Tags(Guid? groupId) => $"immich:{groupId?.ToString() ?? "local"}:tags:all";
}
