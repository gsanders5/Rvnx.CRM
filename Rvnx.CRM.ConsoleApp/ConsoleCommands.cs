using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Rvnx.CRM.ConsoleApp;

internal static class ConsoleCommands
{
    public static async Task<bool> RunCountContactsAsync(IServiceProvider services)
    {
        Core.Interfaces.IRepository repository = services.GetRequiredService<Rvnx.CRM.Core.Interfaces.IRepository>();
        int count = await repository.QueryUnfiltered<Rvnx.CRM.Core.Models.Contact.Contact>()
            .CountAsync(c => !c.IsPartial);
        Console.WriteLine($"Total contacts: {count}");
        return true;
    }

    public static async Task<bool> RunSendDateRemindersAsync(IServiceProvider services)
    {
        Core.Interfaces.IReminderNotificationService service = services.GetRequiredService<Rvnx.CRM.Core.Interfaces.IReminderNotificationService>();
        await service.SendDueRemindersAsync(DateOnly.FromDateTime(DateTime.Today));
        return true;
    }

    public static async Task<bool> RunListUsersAsync(IServiceProvider services)
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

    public static async Task<bool> RunPromoteUserAsync(IServiceProvider services, string[] args)
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

    public static async Task<bool> RunDemoteUserAsync(IServiceProvider services, string[] args)
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
