using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.Models.Base
{
    public abstract class PolymorphicEntity : BaseEntity
    {
        [Required]
        [Display(Name = "Entity ID")]
        public Guid EntityId { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Entity Type")]
        public string EntityType { get; set; } = string.Empty;
    }
}