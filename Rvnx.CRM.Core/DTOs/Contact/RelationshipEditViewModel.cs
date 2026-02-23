using Rvnx.CRM.Core.DTOs.Common;

namespace Rvnx.CRM.Core.DTOs.Contact
{
    public class RelationshipEditViewModel : RelationshipFormDto
    {
        public string EntityName { get; set; } = string.Empty;
        public IEnumerable<SelectOptionDto> RelationshipTypeOptions { get; set; } = [];
        public IEnumerable<SelectOptionDto> RelatedEntityOptions { get; set; } = [];
        public string SelectedRelationshipType { get; set; } = string.Empty;
    }
}
