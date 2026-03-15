using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Contact;

[Table("ContactLabel")]
public class ContactLabel : BaseEntity
{
    public Guid ContactId { get; set; }
    public virtual Contact Contact { get; set; } = null!;

    public Guid LabelId { get; set; }
    public virtual Label Label { get; set; } = null!;
}