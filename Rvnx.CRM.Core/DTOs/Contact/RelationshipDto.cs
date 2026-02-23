using Rvnx.CRM.Core.DTOs.Base;
namespace Rvnx.CRM.Core.DTOs.Contact
{
    public class RelationshipDto : BaseDto
    {
        public Guid EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;

        public Guid? RelatedEntityId { get; set; }
        public string RelatedEntityName { get; set; } = string.Empty;

        public Guid RelationshipTypeId { get; set; }
        public string RelationshipTypeName { get; set; } = string.Empty;
        public string RelationshipTypeOppositeName { get; set; } = string.Empty;
        public string RelationshipTypeCategory { get; set; } = string.Empty;

        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string? PartialContactFirstName { get; set; }
        public string? PartialContactLastName { get; set; }
        public DateTime? PartialContactDateOfBirth { get; set; }
        public bool IsPartialContact { get; set; }
        public bool IsTypeReverse { get; set; }
    }
}
