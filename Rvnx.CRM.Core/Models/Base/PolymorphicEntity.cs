using Rvnx.CRM.Core.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.Models.Base;

public abstract class PolymorphicEntity : BaseEntity
{
    [Required]
    [Display(Name = "Entity ID")]
    public Guid EntityId { get; set; }

    [Required]
    [Display(Name = "Entity Type")]
    public EntityType EntityType { get; set; } = EntityType.Person;
}