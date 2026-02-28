using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;

namespace Rvnx.CRM.Infrastructure.Services;

using Rvnx.CRM.Infrastructure.Data;

public class ReminderNotificationService(IRepository repository, CRMDbContext context) : IReminderNotificationService
{
    private readonly IRepository _repository = repository;
    private readonly CRMDbContext _context = context;

    public async Task<OperationResult> SendDueRemindersAsync(DateOnly forDate)
    {
        List<ReminderOffset> offsets = await _repository.QueryUnfiltered<ReminderOffset>()
            .Include(ro => ro.SignificantDate)
            .Include(ro => ro.ReminderLogs)
            .Where(ro => ro.IsActive && ro.SignificantDate != null && ro.SignificantDate.IsActive)
            .ToListAsync();

        int sentCount = 0;
        int failedCount = 0;

        foreach (ReminderOffset offset in offsets)
        {
            DateOnly nextOccurrence = DateCalculationService.GetNextOccurrence(offset.SignificantDate!, forDate);
            DateOnly scheduledFor = nextOccurrence.AddDays(-offset.DaysBeforeEvent);

            if (scheduledFor != forDate)
            {
                continue;
            }

            if (offset.ReminderLogs.Any(rl => rl.OccurrenceDate == nextOccurrence && rl.Success))
            {
                continue; // Already successfully sent
            }

            // Attempt send (mocked as success)
            bool success = true;
            string? errorMessage = null;

            ReminderLog log = new()
            {
                Id = Guid.NewGuid(),
                ReminderOffsetId = offset.Id,
                OccurrenceDate = nextOccurrence,
                ScheduledFor = scheduledFor,
                SentAt = success ? DateTime.UtcNow : null,
                Success = success,
                ErrorMessage = errorMessage
            };

            await _repository.AddAsync(log);

            if (success)
            {
                sentCount++;
            }
            else
            {
                failedCount++;
            }
        }

        await _repository.SaveChangesAsync();
        return OperationResult.Ok(Guid.Empty, "System");
    }
}
