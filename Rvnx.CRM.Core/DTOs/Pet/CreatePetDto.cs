using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.DTOs.Pet
{
    public class CreatePetDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Species { get; set; }

        [MaxLength(100)]
        public string? Breed { get; set; }

        public DateTime? Birthday { get; set; }

        public string? Notes { get; set; }

        [Required]
        public Guid EntityId { get; set; }
    }
}
