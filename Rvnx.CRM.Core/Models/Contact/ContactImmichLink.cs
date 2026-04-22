using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.Models.Contact;

public class ContactImmichLink : BaseEntity
{
    public Guid ContactId { get; set; }

    public Contact? Contact { get; set; }

    public Guid? ImmichPersonId { get; set; }

    [MaxLength(256)]
    public string? ImmichPersonName { get; set; }

    public Guid? ImmichTagId { get; set; }

    [MaxLength(256)]
    public string? ImmichTagValue { get; set; }
}
