using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.Models.Contact;

public class Address : BaseEntity
{
    public Guid? ContactId { get; set; }

    public virtual Contact? Contact { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Street Address")]
    public string Street { get; set; } = string.Empty;

    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(100)]
    public string State { get; set; } = string.Empty;

    [MaxLength(20)]
    [Display(Name = "Zip Code")]
    public string Zip { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Display(Name = "Address Type")]
    public string AddressType { get; set; } = "Home"; // e.g. Home, Work, Other
}