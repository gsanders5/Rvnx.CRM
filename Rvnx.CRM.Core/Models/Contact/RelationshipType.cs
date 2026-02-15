using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Contact;

[Table("RelationshipType")]
public class RelationshipType : CRMBaseEntity, IGlobalEntity
{
    [Required]
    [MaxLength(100)]
    [Display(Name = "Forward Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Display(Name = "Reverse Name")]
    public string OppositeName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Display(Name = "Entity Type")]
    public string EntityType { get; set; } = string.Empty;

    public bool IsSymmetric => Name == OppositeName;
}
