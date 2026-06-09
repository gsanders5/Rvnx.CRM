using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rvnx.CRM.Infrastructure;

namespace Rvnx.CRM.ConsoleApp;

internal static class TaskManager
{
    private static readonly Action<ILogger, string, Exception?> LogTaskStarting =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1), "Starting task: {TaskName}");

    private static readonly Action<ILogger, string, Exception?> LogTaskCompleted =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(2), "Completed task: {TaskName}");

    private static readonly Action<ILogger, string, Exception?> LogTaskFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(3), "Task {TaskName} failed");

    public static async Task<int> ProcessAsync(string[] args)
    {
        string taskName = args[0].ToUpperInvariant();

        using IHost host = AppHost.Build();
        ILogger logger = host.Services.GetRequiredService<ILogger<Program>>();

        if (!await host.Services.ApplyDatabaseMigrationsAsync(logger))
        {
            return 1;
        }

        LogTaskStarting(logger, taskName, null);

        try
        {
            using IServiceScope scope = host.Services.CreateScope();
            IServiceProvider services = scope.ServiceProvider;

            bool success = taskName switch
            {
                "COUNT-CONTACTS" => await ConsoleCommands.RunCountContactsAsync(services),
                "SEND-DATE-REMINDERS" => await ConsoleCommands.RunSendDateRemindersAsync(services),
                "LIST-USERS" => await ConsoleCommands.RunListUsersAsync(services),
                "PROMOTE-USER" => await ConsoleCommands.RunPromoteUserAsync(services, args),
                "DEMOTE-USER" => await ConsoleCommands.RunDemoteUserAsync(services, args),
                "MERGE-USERS" => await ConsoleCommands.RunMergeUsersAsync(services, args),
                "ADD-API-TOKEN" => await ConsoleCommands.RunAddApiTokenAsync(services, args),
                "REVOKE-API-TOKEN" => await ConsoleCommands.RunRevokeApiTokenAsync(services, args),
                _ => false
            };

            if (success)
            {
                LogTaskCompleted(logger, taskName, null);
                return 0;
            }

            Console.WriteLine($"Unknown task: {taskName}");
            return 1;
        }
        catch (Exception ex)
        {
            LogTaskFailed(logger, taskName, ex);
            return 1;
        }
    }
}
