namespace Rvnx.CRM.Web.ViewModels.Immich;

public sealed record ImmichGalleryRequest
{
    public Guid ContactId { get; init; }
    public Guid? PersonId { get; init; }
    public string? PersonName { get; init; }
    public Guid? TagId { get; init; }
    public string? TagValue { get; init; }
}
