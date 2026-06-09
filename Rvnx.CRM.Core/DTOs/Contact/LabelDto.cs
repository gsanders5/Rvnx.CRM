namespace Rvnx.CRM.Core.DTOs.Contact;

public class LabelDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }

    /// <summary>
    /// Number of contacts tagged with this label. Only populated by
    /// <see cref="Rvnx.CRM.Core.Interfaces.ILabelService.GetAllAsync"/>; other callers leave this at 0.
    /// </summary>
    public int ContactCount { get; set; }
}
