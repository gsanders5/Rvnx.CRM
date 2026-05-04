using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class RelationshipFormViewModel : RelationshipFormDto
{
    public string ContactName { get; set; } = string.Empty;
    public IEnumerable<SelectOptionDto> RelationshipTypeOptions { get; set; } = [];
    public List<RelationshipTypeDefinition> RelationshipTypes { get; set; } = [];
    public IEnumerable<SelectOptionDto> RelatedContactOptions { get; set; } = [];
    public string SelectedRelationshipType { get; set; } = string.Empty;
    public bool IsContactPartial { get; set; }
    public bool IsRelatedContactPartial { get; set; }
}
