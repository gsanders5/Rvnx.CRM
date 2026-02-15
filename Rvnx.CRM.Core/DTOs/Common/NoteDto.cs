using Rvnx.CRM.Core.DTOs.Common;
using System;

namespace Rvnx.CRM.Core.DTOs.Common
{
    public class NoteDto : BaseDto
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
    }
}
