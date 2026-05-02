using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using System.Globalization;

namespace Rvnx.CRM.Infrastructure.Services;

public class ReminderNotificationService(
    IRepository repository,
    IConfiguration configuration) : IReminderNotificationService
{
    private readonly IRepository _repository = repository;
    private readonly IConfiguration _configuration = configuration;

    public async Task<string> SendDueRemindersAsync(DateOnly forDate)
    {
        IConfigurationSection emailConfig = _configuration.GetSection("EmailNotifications");
        bool isEnabled = bool.TryParse(emailConfig["Enabled"], out bool result) && result;

        if (!isEnabled)
        {
            return "Email notifications are disabled in configuration.";
        }

        List<ReminderOffset> offsets = await _repository.QueryUnfiltered<ReminderOffset>()
            .Include(ro => ro.SignificantDate)
            .ThenInclude(sd => sd!.Contact)
            .Include(ro => ro.ReminderLogs)
            .Where(ro => ro.IsActive
                && ro.SignificantDate != null
                && ro.SignificantDate.IsActive
                && (ro.SignificantDate.Contact == null || !ro.SignificantDate.Contact.IsDeceased))
            .AsSplitQuery()
            .ToListAsync();

        int sentCount = 0;
        int failedCount = 0;
        Dictionary<Guid, List<string>> emailsByGroupId = [];

        foreach (ReminderOffset offset in offsets)
        {
            DateOnly nextOccurrence = DateCalculationService.GetNextOccurrence(offset.SignificantDate!, forDate);
            DateOnly scheduledFor = nextOccurrence.AddDays(-offset.DaysBeforeEvent);

            if (scheduledFor != forDate)
            {
                continue;
            }

            ReminderLog? existingLog = offset.ReminderLogs.FirstOrDefault(rl => rl.OccurrenceDate == nextOccurrence);

            if (existingLog != null && existingLog.Success)
            {
                continue;
            }

            Guid? contactGroupId = offset.SignificantDate?.Contact?.GroupId;
            List<string> recipientEmails = [];

            if (contactGroupId.HasValue)
            {
                if (!emailsByGroupId.TryGetValue(contactGroupId.Value, out List<string>? cached))
                {
                    cached = await _repository.QueryUnfiltered<User>()
                        .Where(u => u.GroupId == contactGroupId.Value && !string.IsNullOrEmpty(u.Email))
                        .Select(u => u.Email)
                        .ToListAsync();
                    emailsByGroupId[contactGroupId.Value] = cached;
                }
                recipientEmails = cached;
            }

            if (recipientEmails.Count == 0)
            {
                continue;
            }

            (bool success, string? errorMessage) =
                await SendEmailAsync(offset, nextOccurrence, recipientEmails, emailConfig);

            if (existingLog != null)
            {
                existingLog.SentAt = success ? DateTime.UtcNow : null;
                existingLog.Success = success;
                existingLog.ErrorMessage = errorMessage;
                await _repository.UpdateAsync(existingLog);
            }
            else
            {
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
            }

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
        return $"Processed: {sentCount} sent, {failedCount} failed.";
    }

    private static async Task<(bool Success, string? Error)> SendEmailAsync(
        ReminderOffset offset,
        DateOnly occurrenceDate,
        List<string> recipientEmails,
        IConfigurationSection emailConfig)
    {
        try
        {
            IConfigurationSection smtpSettings = emailConfig.GetSection("SmtpSettings");
            MimeMessage message = BuildMimeMessage(offset, occurrenceDate, recipientEmails, smtpSettings);

            using SmtpClient client = new();

            string portStr = smtpSettings["Port"] ?? "587";
            int port = int.Parse(portStr, CultureInfo.InvariantCulture);

            await client.ConnectAsync(
                smtpSettings["Server"] ?? "localhost",
                port,
                SecureSocketOptions.StartTls);

            if (!string.IsNullOrEmpty(smtpSettings["Username"]))
            {
                await client.AuthenticateAsync(smtpSettings["Username"]!, smtpSettings["Password"]!);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static MimeMessage BuildMimeMessage(
        ReminderOffset offset,
        DateOnly occurrenceDate,
        List<string> recipientEmails,
        IConfigurationSection smtpSettings)
    {
        string senderEmail = smtpSettings["SenderEmail"] ??
                             throw new InvalidOperationException("SenderEmail is not configured.");
        string senderName = smtpSettings["SenderName"] ?? "Rvnx CRM";

        Core.Models.Contact.Contact? contact = offset.SignificantDate?.Contact;
        string contactName = contact != null
            ? $"{contact.FirstName} {contact.LastName}".Trim()
            : "Unknown Contact";

        if (string.IsNullOrWhiteSpace(contactName))
        {
            contactName = "Unknown Contact";
        }

        string title = offset.SignificantDate?.Title ?? "Significant Date";
        string dateFormatted = occurrenceDate.ToString("D", CultureInfo.CurrentCulture);

        MimeMessage message = new();
        message.From.Add(new MailboxAddress(senderName, senderEmail));

        foreach (string email in recipientEmails)
        {
            message.To.Add(new MailboxAddress(string.Empty, email));
        }

        message.Subject = $"Reminder: {title} for {contactName}";

        BodyBuilder bodyBuilder = new()
        {
            TextBody = $"Reminder: {title} for {contactName} is on {dateFormatted}.",
            HtmlBody = $@"
            <div style=""font-family: sans-serif; padding: 20px; color: #333; line-height: 1.5;"">
                <h2 style=""color: #2c3e50; border-bottom: 1px solid #eee; padding-bottom: 10px;"">CRM Notification</h2>
                <p>This is a reminder for an upcoming significant date:</p>
                <div style=""background-color: #f8f9fa; padding: 15px; border-radius: 5px; border-left: 4px solid #3498db; margin: 20px 0;"">
                    <strong style=""display: block; font-size: 1.1em;"">{title}</strong>
                    <span style=""color: #7f8c8d;"">Contact: {contactName}</span><br/>
                    <span style=""color: #7f8c8d;"">Date: {dateFormatted}</span>
                </div>
                <p style=""font-size: 0.9em; color: #95a5a6; margin-top: 30px;"">
                    This is an automated message from the Rvnx CRM system provided at your request.
                </p>
            </div>"
        };

        message.Body = bodyBuilder.ToMessageBody();

        return message;
    }
}
