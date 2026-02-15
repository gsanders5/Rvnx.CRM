using System;

namespace Rvnx.CRM.Core.DTOs.Common
{
    public class RelationshipDto
    {
        public Guid Id { get; set; }
        public Guid EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public Guid RelatedEntityId { get; set; }

        // Flattened properties for display
        public string RelationshipTypeName { get; set; } = string.Empty;
        public string RelationshipTypeOppositeName { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string RelatedEntityName { get; set; } = string.Empty;

        public Guid RelationshipTypeId { get; set; }
    }
}
