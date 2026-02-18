using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.DTOs.Contact
{
    public class UpdateContactDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [MaxLength(100)]
        public string? Nickname { get; set; }

        [EmailAddress]
        [MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? JobTitle { get; set; }

        [MaxLength(200)]
        public string? Company { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Birthday { get; set; }
        public bool IsHidden { get; set; }
    }
}
