namespace Rvnx.CRM.Core.DTOs.Contact
{
    public class ContactEditViewModel : ContactFormDto
    {
        public IEnumerable<string> PronounOptions { get; set; } = [];
        public IEnumerable<string> GenderOptions { get; set; } = [];
    }
}
