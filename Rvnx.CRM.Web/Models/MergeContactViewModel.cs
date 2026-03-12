using System.ComponentModel.DataAnnotations;
using Rvnx.CRM.Core.DTOs.Contact;

namespace Rvnx.CRM.Web.Models;

public class MergeContactViewModel
{
    [Required]
    public Guid PrimaryContactId { get; set; }

    [Required]
    public Guid SecondaryContactId { get; set; }

    public ContactDetailDto? PrimaryContact { get; set; }
    public ContactDetailDto? SecondaryContact { get; set; }
}
