
namespace Rvnx.CRM.Core.DTOs.Base;

public record class NoteDto : BaseDto
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
    public Guid ContactId { get; set; }
}
