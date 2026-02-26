using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models;

[Index(nameof(SelfContactId), IsUnique = true)]
public class User : BaseEntity, IGlobalEntity
{
    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string SubjectId { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? DisplayName { get; set; }

    public Guid? SelfContactId { get; set; }

    [ForeignKey(nameof(SelfContactId))]
    public virtual Rvnx.CRM.Core.Models.Contact.Contact? SelfContact { get; set; }

    [ForeignKey(nameof(GroupId))]
    public virtual UserGroup? Group { get; set; }

    public bool IsAdministrator { get; set; }
}
