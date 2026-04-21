using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.Models;

public class UserGroup : BaseEntity, IGlobalEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public ICollection<User> Members { get; set; } = [];
}
