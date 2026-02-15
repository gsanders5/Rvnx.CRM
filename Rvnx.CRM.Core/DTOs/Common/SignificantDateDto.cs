using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.Core.DTOs.Common
{
    public class SignificantDateDto : BaseDto, IRemindableDto
    {
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? Description { get; set; }
        public Guid EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public bool RemindMe { get; set; }
        public DateTime? ReminderSent { get; set; }
        public TimeSpan EventFrequency { get; set; } = TimeSpan.FromDays(365);
    }
}
