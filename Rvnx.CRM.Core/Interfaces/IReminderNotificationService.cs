namespace Rvnx.CRM.Core.Interfaces;

public interface IReminderNotificationService
{
    Task<string> SendDueRemindersAsync(DateOnly forDate);
}
