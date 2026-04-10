using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Base;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class AddressDto : BaseDto
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string AddressType { get; set; } = Constants.AddressTypes.Home;
    public Guid EntityId { get; set; }

    public string FormattedAddress =>
        string.Join(", ", new[] { Street, City, State, Zip, Country }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
}
