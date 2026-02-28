using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                "SEND-DATE-REMINDERS" => await RunSendDateRemindersAsync(services),
                "LIST-USERS" => await RunListUsersAsync(services),
                "PROMOTE-USER" => await RunPromoteUserAsync(services, args),
                "DEMOTE-USER" => await RunDemoteUserAsync(services, args),
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
        Core.Interfaces.IRepository repository = services.GetRequiredService<Rvnx.CRM.Core.Interfaces.IRepository>();
        int count = await repository.QueryUnfiltered<Rvnx.CRM.Core.Models.Contact.Contact>()
            .CountAsync(c => !c.IsPartial);
        Console.WriteLine($"Total contacts: {count}");
        return true;
    }

    private static async Task<bool> RunSendDateRemindersAsync(IServiceProvider services)
    {
        Core.Interfaces.IReminderNotificationService service = services.GetRequiredService<Rvnx.CRM.Core.Interfaces.IReminderNotificationService>();
        await service.SendDueRemindersAsync(DateOnly.FromDateTime(DateTime.Today));
        return true;
    }

    private static async Task<bool> RunListUsersAsync(IServiceProvider services)
    {
        Core.Interfaces.IRepository repository = services.GetRequiredService<Core.Interfaces.IRepository>();
        List<Core.Models.User> users = await repository.QueryUnfiltered<Core.Models.User>().ToListAsync();
        
        Console.WriteLine($"Found {users.Count} users:");
        foreach (Core.Models.User user in users)
        {
            string adminStatus = user.IsAdministrator ? "[Admin]" : "[User]";
            Console.WriteLine($"- {user.Email} {adminStatus} (Name: {user.DisplayName ?? "N/A"})");
        }
        
        return true;
    }

    private static async Task<bool> RunPromoteUserAsync(IServiceProvider services, string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Error: Please provide the user's email address. Usage: PROMOTE-USER <email>");
            return false;
        }

        string email = args[1];
        Core.Interfaces.IRepository repository = services.GetRequiredService<Core.Interfaces.IRepository>();
        
        Core.Models.User? user = await repository.QueryUnfiltered<Core.Models.User>()
            .FirstOrDefaultAsync(u => EF.Functions.Like(u.Email, email));

        if (user == null)
        {
            Console.WriteLine($"Error: User with email '{email}' not found.");
            return false;
        }

        if (user.IsAdministrator)
        {
            Console.WriteLine($"User '{email}' is already an administrator.");
            return true;
        }

        user.IsAdministrator = true;
        await repository.UpdateAsync(user);
        await repository.SaveChangesAsync();
        
        Console.WriteLine($"Successfully promoted user '{email}' to administrator.");
        return true;
    }

    private static async Task<bool> RunDemoteUserAsync(IServiceProvider services, string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Error: Please provide the user's email address. Usage: DEMOTE-USER <email>");
            return false;
        }

        string email = args[1];
        Core.Interfaces.IRepository repository = services.GetRequiredService<Core.Interfaces.IRepository>();
        
        Core.Models.User? user = await repository.QueryUnfiltered<Core.Models.User>()
            .FirstOrDefaultAsync(u => EF.Functions.Like(u.Email, email));

        if (user == null)
        {
            Console.WriteLine($"Error: User with email '{email}' not found.");
            return false;
        }

        if (!user.IsAdministrator)
        {
            Console.WriteLine($"User '{email}' is already not an administrator.");
            return true;
        }

        user.IsAdministrator = false;
        await repository.UpdateAsync(user);
        await repository.SaveChangesAsync();
        
        Console.WriteLine($"Successfully demoted user '{email}' from administrator.");
        return true;
    }
}