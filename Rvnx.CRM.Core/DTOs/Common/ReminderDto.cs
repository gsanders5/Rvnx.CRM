using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.Core.DTOs.Common
{
    public class ReminderDto : BaseDto, IRemindableDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsCompleted { get; set; }
        public Guid EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public bool RemindMe { get; set; }
        public DateTime? ReminderSent { get; set; }
        public TimeSpan EventFrequency { get; set; } = TimeSpan.FromDays(365);
    }
}
