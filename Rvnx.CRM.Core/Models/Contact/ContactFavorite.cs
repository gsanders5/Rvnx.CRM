using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Contact;

[Table("ContactFavorite")]
public class ContactFavorite : BaseEntity
{
    public Guid ContactId { get; set; }
    public virtual Contact Contact { get; set; } = null!;
}
