using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Contact;

namespace Rvnx.CRM.Core.DTOs.Contact
{
    public class RelationshipCreateViewModel : RelationshipFormDto
    {
        public string EntityName { get; set; } = string.Empty;
        public IEnumerable<SelectOptionDto> RelationshipTypeOptions { get; set; } = new List<SelectOptionDto>();
        public IEnumerable<SelectOptionDto> RelatedEntityOptions { get; set; } = new List<SelectOptionDto>();
        public string SelectedRelationshipType { get; set; } = string.Empty;
    }
}
