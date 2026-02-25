namespace Rvnx.CRM.Core.DTOs.Contact
{
    public class RelationshipDeleteViewModel : RelationshipDto
    {
        // EntityName is inherited from RelationshipDto
        public string? ReturnUrl { get; set; }
    }
}
