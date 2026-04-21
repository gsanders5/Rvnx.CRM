using Rvnx.CRM.Core.DTOs.Base;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class AddressDto : BaseDto
{
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string AddressType { get; set; } = Constants.AddressTypes.Home;
    public Guid EntityId { get; set; }

    public string FormattedAddress =>
        string.Join(", ", new[] { Line1, Line2, City, State, Zip, Country }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
}
