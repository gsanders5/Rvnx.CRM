namespace Rvnx.CRM.Core.DTOs.Contact;

public class SuggestedRelationshipDto
{
    public string Payload { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string RelationshipName { get; set; } = string.Empty;
}
