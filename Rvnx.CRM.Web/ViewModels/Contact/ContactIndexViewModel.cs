using Rvnx.CRM.Core.DTOs.Contact;

namespace Rvnx.CRM.Web.ViewModels.Contact;

public class ContactIndexViewModel
{
    public IEnumerable<ContactDto> Contacts { get; set; } = [];
    public List<LabelDto> AllLabels { get; set; } = [];
}
