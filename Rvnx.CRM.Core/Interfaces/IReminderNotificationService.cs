using Rvnx.CRM.Core.Models;

namespace Rvnx.CRM.Core.Interfaces;

public interface IReminderNotificationService
{
    Task<OperationResult> SendDueRemindersAsync(DateOnly forDate);
}