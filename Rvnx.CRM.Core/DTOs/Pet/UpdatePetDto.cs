using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.DTOs.Pet
{
    public class UpdatePetDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Species { get; set; }

        [MaxLength(100)]
        public string? Breed { get; set; }

        public DateTime? Birthday { get; set; }

        public string? Notes { get; set; }
    }
}
