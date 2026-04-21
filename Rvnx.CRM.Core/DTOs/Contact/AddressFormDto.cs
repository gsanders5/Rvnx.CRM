using Rvnx.CRM.Core.Constants;
using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class AddressFormDto
{
    public Guid? Id { get; set; }

    public Guid EntityId { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Address Line 1")]
    public string Line1 { get; set; } = string.Empty;

    [MaxLength(200)]
    [Display(Name = "Address Line 2")]
    public string? Line2 { get; set; }

    [MaxLength(100)]
    [Display(Name = "City")]
    public string City { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "State / Province")]
    public string State { get; set; } = string.Empty;

    [MaxLength(20)]
    [Display(Name = "Zip / Postal Code")]
    public string Zip { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "Country")]
    public string Country { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Display(Name = "Address Type")]
    public string AddressType { get; set; } = AddressTypes.Home;
}
