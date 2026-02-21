using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Base;

namespace Rvnx.CRM.Core.DTOs.Base
{
    public class NoteDeleteViewModel : NoteDto
    {
        public string EntityName { get; set; } = string.Empty;
    }
}
