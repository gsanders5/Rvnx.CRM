namespace Rvnx.CRM.Core.Models.Base;

public abstract class RemindableEntity : PolymorphicEntity
{
    public bool RemindMe { get; set; } = false;
    public DateTime? ReminderSent { get; set; }
    public TimeSpan EventFrequency { get; set; } = TimeSpan.FromDays(365);
}
