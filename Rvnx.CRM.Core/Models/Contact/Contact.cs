using Rvnx.CRM.Core.Models.Base;
using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Models.Business;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Contact
{
    [Table("Contact")]
    [Index(nameof(LinkedUserId), IsUnique = true)]
    public class Contact : Person
    {
        [Display(Name = "Employers")]
        [InverseProperty(nameof(Employer.Employee))]
        public virtual ICollection<Employer> Employers { get; set; } = [];

        public Guid? LinkedUserId { get; set; }

        [ForeignKey(nameof(LinkedUserId))]
        public virtual Rvnx.CRM.Core.Models.User? LinkedUser { get; set; }
    }
}
