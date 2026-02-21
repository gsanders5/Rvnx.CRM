using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.DTOs.Contact
{
    public class RelationshipFormDto
    {
        public Guid? Id { get; set; }
        public Guid EntityId { get; set; }
        public Guid RelatedEntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;

        [Required]
        public Guid RelationshipTypeId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }
    }
}
