namespace Rvnx.CRM.Core.DTOs.Contact;

public class ContactExportResult
{
    public byte[] FileContent { get; set; } = [];
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/vcard";
}
