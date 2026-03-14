using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.DTOs.Base
{
    public class NoteFormDto
    {
        public Guid? Id { get; set; }

        [Required]
        [MaxLength(200)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Content")]
        public string Value { get; set; } = string.Empty;

        public Guid EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
    }
}