using Rvnx.CRM.Core.DTOs.Immich;

namespace Rvnx.CRM.Core.Interfaces;

/// <summary>
/// CRUD for the current group's Immich server settings. All reads/writes are scoped to the
/// caller's group by the repository's global query filter, so one stored row is shared by
/// every member of the group.
/// </summary>
public interface IImmichSettingsService
{
    /// <summary>Display-safe settings for the current group, or null when none are stored.</summary>
    Task<ImmichSettingsDto?> GetSettingsAsync(CancellationToken ct = default);

    /// <summary>Full connection details (including the raw API key) for the current group, or null when none are stored.</summary>
    Task<ImmichConnectionDto?> GetConnectionAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates or updates the current group's settings. On update, a null/blank
    /// <paramref name="apiKey"/> keeps the existing key; on create it is required.
    /// </summary>
    Task<ImmichSettingsOperationResult> SaveAsync(bool enabled, string baseUrl, string? apiKey, CancellationToken ct = default);

    /// <summary>Removes the current group's settings entirely.</summary>
    Task<ImmichSettingsOperationResult> DeleteAsync(CancellationToken ct = default);
}
