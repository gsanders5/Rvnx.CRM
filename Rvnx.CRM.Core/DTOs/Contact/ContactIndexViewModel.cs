namespace Rvnx.CRM.Core.DTOs.Contact
{
    public class ContactIndexViewModel
    {
        public IEnumerable<ContactDto> Contacts { get; set; } = new List<ContactDto>();
        public string? SuccessMessage { get; set; }
    }
}
