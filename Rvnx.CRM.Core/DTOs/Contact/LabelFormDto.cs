using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.DTOs.Contact
{
    public class LabelFormDto
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(7)]
        public string? Color { get; set; }
    }
}
