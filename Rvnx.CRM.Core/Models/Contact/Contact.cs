using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Business;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Contact
{
    [Table("Contact")]
    public class Contact : Person
    {
        [Display(Name = "Employers")]
        [InverseProperty(nameof(Employer.Employee))]
        public virtual ICollection<Employer> Employers { get; set; } = [];
    }
}
