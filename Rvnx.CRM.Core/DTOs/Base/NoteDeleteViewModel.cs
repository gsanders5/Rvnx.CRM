namespace Rvnx.CRM.Core.DTOs.Base;

public record class NoteDeleteViewModel : NoteDto
{
    public string ContactName { get; init; } = string.Empty;

    public NoteDeleteViewModel(NoteDto dto, string contactName) : base(dto)
    {
        ContactName = contactName;
    }
}
