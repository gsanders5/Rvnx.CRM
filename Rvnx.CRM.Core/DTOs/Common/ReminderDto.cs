using System;

namespace Rvnx.CRM.Core.DTOs.Common
{
    public class ReminderDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public Guid EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
    }
}
