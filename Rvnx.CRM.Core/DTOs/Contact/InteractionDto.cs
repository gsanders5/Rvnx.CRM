namespace Rvnx.CRM.Core.DTOs.Contact;

public class InteractionDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public string Icon { get; set; } = "bi-circle";
}
