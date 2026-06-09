using Rvnx.CRM.Core.DTOs.Base;

namespace Rvnx.CRM.Web.ViewModels.Immich;

public sealed class ImmichGalleryViewModel
{
    public Guid ContactId { get; init; }
    public Guid? PersonId { get; init; }
    public string? PersonName { get; init; }
    public Guid? TagId { get; init; }
    public string? TagValue { get; init; }
    public string? WebBaseUrl { get; init; }
    public IReadOnlyList<ImmichAssetDto> Assets { get; init; } = [];
}
