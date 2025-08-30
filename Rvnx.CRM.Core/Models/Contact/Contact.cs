using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Dates;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Contact
{
    [Table("Contact")]
    public class Contact : Person
    {
        [Display(Name = "Phone Numbers")]
        [InverseProperty(nameof(PhoneNumber.Person))]
        public virtual ICollection<PhoneNumber> PhoneNumbers { get; set; } = [];

        [Display(Name = "Notes")]
        [InverseProperty(nameof(Note.Person))]
        public virtual ICollection<Note> Notes { get; set; } = [];

        [Display(Name = "Employers")]
        [InverseProperty(nameof(Employer.Employee))]
        public virtual ICollection<Employer> Employers { get; set; } = [];
    }
}
