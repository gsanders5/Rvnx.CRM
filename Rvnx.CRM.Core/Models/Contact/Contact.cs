using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Business;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Contact
{
    [Table("Contact")]
    public class Contact : Person
    {
        public bool IsPartial { get; set; }

        [MaxLength(100)]
        public string? Pronouns { get; set; }

        [MaxLength(100)]
        public string? Gender { get; set; }

        [MaxLength(100)]
        public string? Religion { get; set; }

        public virtual ICollection<Pet> Pets { get; set; } = [];

        [Display(Name = "Employers")]
        [InverseProperty(nameof(Employer.Employee))]
        public virtual ICollection<Employer> Employers { get; set; } = [];

        public virtual ICollection<ContactLabel> ContactLabels { get; set; } = [];
    }
}