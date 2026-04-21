using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Contact;

[Table("PetContact")]
public class PetContact : BaseEntity
{
    public Guid PetId { get; set; }
    public virtual Pet Pet { get; set; } = null!;

    public Guid ContactId { get; set; }
    public virtual Contact Contact { get; set; } = null!;
}
