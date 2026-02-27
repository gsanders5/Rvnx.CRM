using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rvnx.CRM.Infrastructure.Data;

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

        if (!await AppHost.MigrateDatabaseAsync(host, logger))
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
                "COUNT-CONTACTS" => await RunCountContactsAsync(services),
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

    private static async Task<bool> RunCountContactsAsync(IServiceProvider services)
    {
        CRMDbContext context = services.GetRequiredService<CRMDbContext>();
        int count = await context.Contacts
            .IgnoreQueryFilters()
            .CountAsync(c => !c.IsPartial);
        Console.WriteLine($"Total contacts: {count}");
        return true;
    }
}