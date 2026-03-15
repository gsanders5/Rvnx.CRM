namespace Rvnx.CRM.Core.DTOs.Contact;

public class ContactCreateViewModel : ContactFormDto
{
    public bool IsSelfCreate { get; set; }
    public IEnumerable<string> PronounOptions { get; set; } = [];
    public IEnumerable<string> GenderOptions { get; set; } = [];
}