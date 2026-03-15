namespace Rvnx.CRM.Core.DTOs.Common;

public class SelectOptionDto
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool Selected { get; set; }
    public string? Group { get; set; }
}