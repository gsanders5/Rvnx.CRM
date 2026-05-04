using Rvnx.CRM.Core.DTOs.Base;

namespace Rvnx.CRM.Core.DTOs.Contact;

public record class RelationshipDto : BaseDto
{
    public Guid ContactId { get; set; }
    public string ContactName { get; set; } = string.Empty;

    public Guid RelatedContactId { get; set; }
    public string RelatedContactName { get; set; } = string.Empty;

    public Guid RelationshipTypeId { get; set; }
    public string RelationshipTypeName { get; set; } = string.Empty;
    public string RelationshipTypeOppositeName { get; set; } = string.Empty;
    public string RelationshipTypeCategory { get; set; } = string.Empty;

    public bool IsContactPartial { get; set; }
    public bool IsRelatedContactPartial { get; set; }

    public bool IsContactDeceased { get; set; }
    public bool IsRelatedContactDeceased { get; set; }

    public string? Description { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
