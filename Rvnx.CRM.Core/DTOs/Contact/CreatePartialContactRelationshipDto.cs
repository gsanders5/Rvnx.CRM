using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.DTOs.Contact
{
    public class CreatePartialContactRelationshipDto
    {
        [Required]
        public string SelectedRelationshipType { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string PartialContactFirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? PartialContactLastName { get; set; }

        public DateTime? Birthday { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public List<string> SuggestedRelationships { get; set; } = [];
    }
}