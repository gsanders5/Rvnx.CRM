namespace Rvnx.CRM.Core.DTOs.Contact
{
    public class ContactIndexViewModel
    {
        public IEnumerable<ContactDto> Contacts { get; set; } = [];
        public string? SuccessMessage { get; set; }
    }
}
