using Rvnx.CRM.Core.DTOs.Contact;
using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Web.ViewModels.Merge;

public class MergeContactViewModel
{
    [Required]
    public Guid PrimaryContactId { get; set; }

    [Required]
    public Guid SecondaryContactId { get; set; }

    public ContactDetailDto? PrimaryContact { get; set; }
    public ContactDetailDto? SecondaryContact { get; set; }
}
