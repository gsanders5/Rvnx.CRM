using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Contact;

[Table("Address")]
public class Address : BaseEntity
{
    public Guid? ContactId { get; set; }

    [ForeignKey(nameof(ContactId))]
    public virtual Contact? Contact { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Address Line 1")]
    public string Line1 { get; set; } = string.Empty;

    [MaxLength(200)]
    [Display(Name = "Address Line 2")]
    public string? Line2 { get; set; }

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
    public string AddressType { get; set; } = Constants.AddressTypes.Home;
}
