namespace Rvnx.CRM.Core.DTOs.Immich;

/// <summary>
/// Full Immich connection details for the current group, including the raw API key.
/// Internal plumbing between the settings store and <see cref="Interfaces.IImmichService"/>;
/// never hand this to a view.
/// </summary>
public sealed record ImmichConnectionDto(Guid? GroupId, bool Enabled, string BaseUrl, string ApiKey);
