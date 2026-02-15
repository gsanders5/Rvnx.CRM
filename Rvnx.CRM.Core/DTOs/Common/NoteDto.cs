using System;

namespace Rvnx.CRM.Core.DTOs.Common
{
    public class NoteDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public Guid EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
    }
}
