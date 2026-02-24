using Rvnx.CRM.Core.DTOs.Contact;

namespace Rvnx.CRM.Core.DTOs.DataTable;

public class ContactDataTableDto : ContactDto
{
    public string PhotoHtml { get; set; } = string.Empty;
    public string NameHtml { get; set; } = string.Empty;
    public string ActionsHtml { get; set; } = string.Empty;
}
