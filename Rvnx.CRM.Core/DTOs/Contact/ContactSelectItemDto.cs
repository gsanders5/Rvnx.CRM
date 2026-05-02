namespace Rvnx.CRM.Core.DTOs.Contact;

/// <summary>
/// Lightweight Id/Name pair used to populate searchable contact dropdowns
/// (e.g. selecting an introducer for "How we met").
/// </summary>
public class ContactSelectItemDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
}
