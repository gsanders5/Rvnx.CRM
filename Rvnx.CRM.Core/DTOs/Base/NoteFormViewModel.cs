using Rvnx.CRM.Core.Enumerations;

namespace Rvnx.CRM.Core.DTOs.Base;

public class NoteFormViewModel : NoteFormDto
{
    public EntityType EntityType { get; set; } = EntityType.Person;
    public string EntityName { get; set; } = string.Empty;
}
