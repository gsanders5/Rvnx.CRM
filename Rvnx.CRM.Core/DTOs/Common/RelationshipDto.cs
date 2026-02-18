namespace Rvnx.CRM.Core.DTOs.Common
{
    public class RelationshipDto : BaseDto
    {
        public Guid EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;

        public Guid RelatedEntityId { get; set; }
        public string RelatedEntityName { get; set; } = string.Empty;

        public Guid RelationshipTypeId { get; set; }
        public string RelationshipTypeName { get; set; } = string.Empty;
        public string RelationshipTypeOppositeName { get; set; } = string.Empty;
        public string RelationshipTypeCategory { get; set; } = string.Empty;

        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
