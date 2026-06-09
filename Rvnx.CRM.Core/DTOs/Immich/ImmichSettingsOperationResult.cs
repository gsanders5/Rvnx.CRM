using Rvnx.CRM.Core.DTOs.Base;

namespace Rvnx.CRM.Core.DTOs.Immich;

public class ImmichSettingsOperationResult : EntityOperationResult
{
    public Guid? SettingsId { get; set; }

    public static ImmichSettingsOperationResult Failure(params string[] errors)
    {
        return new ImmichSettingsOperationResult { Success = false, Errors = errors.ToList() };
    }

    public static ImmichSettingsOperationResult Ok(Guid settingsId)
    {
        return new ImmichSettingsOperationResult { Success = true, SettingsId = settingsId };
    }

    public static ImmichSettingsOperationResult NotFound(string error = "Immich settings not found.")
    {
        return new ImmichSettingsOperationResult { Success = false, IsNotFound = true, Errors = { error } };
    }
}
