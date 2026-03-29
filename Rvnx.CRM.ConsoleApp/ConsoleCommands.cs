using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.ConsoleApp;

internal static class ConsoleCommands
{
    public static async Task<bool> RunCountContactsAsync(IServiceProvider services)
    {
        IRepository repository = services.GetRequiredService<IRepository>();
        int count = await repository.QueryUnfiltered<Contact>()
            .CountAsync(c => !c.IsPartial);
        Console.WriteLine($"Total contacts: {count}");
        return true;
    }

    public static async Task<bool> RunSendDateRemindersAsync(IServiceProvider services)
    {
        IReminderNotificationService service =
            services.GetRequiredService<IReminderNotificationService>();
        await service.SendDueRemindersAsync(DateOnly.FromDateTime(DateTime.Today));
        return true;
    }

    public static async Task<bool> RunListUsersAsync(IServiceProvider services)
    {
        IRepository repository = services.GetRequiredService<IRepository>();
        List<User> users = await repository.QueryUnfiltered<User>().ToListAsync();

        Console.WriteLine($"Found {users.Count} users:");
        foreach (User user in users)
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
        IRepository repository = services.GetRequiredService<IRepository>();

        User? user = await repository.QueryUnfiltered<User>()
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

    /// <summary>
    /// Usage:
    ///   MERGE-USERS                                    — list all users
    ///   MERGE-USERS &lt;email1&gt; &lt;email2&gt;                  — dry-run (shows what would happen)
    ///   MERGE-USERS &lt;email1&gt; &lt;email2&gt; --confirm        — actually performs the merge
    ///
    /// email1 is the TARGET (keeps the data); email2 is the SOURCE (is deleted).
    /// </summary>
    public static async Task<bool> RunAddApiTokenAsync(IServiceProvider services, string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Error: Please provide the user's email address and token name. Usage: ADD-API-TOKEN <email> <token-name>");
            return false;
        }

        string email = args[1];
        string tokenName = args[2];
        IRepository repository = services.GetRequiredService<IRepository>();

        User? user = await repository.QueryUnfiltered<User>()
            .FirstOrDefaultAsync(u => EF.Functions.Like(u.Email, email));

        if (user == null)
        {
            Console.WriteLine($"Error: User with email '{email}' not found.");
            return false;
        }

        if (user.GroupId == null)
        {
            Console.WriteLine($"Error: User with email '{email}' does not belong to a group.");
            return false;
        }

        IApiTokenService tokenService = services.GetRequiredService<IApiTokenService>();

        // Optional expiration date could be added if needed, for now we set it to null (no expiration)
        (ApiToken token, string rawToken) = await tokenService.CreateTokenAsync(user.Id, user.GroupId.Value, tokenName, null);

        Console.WriteLine($"Successfully created API token '{tokenName}' for user '{email}'.");
        Console.WriteLine($"Raw Token: {rawToken}");
        Console.WriteLine("IMPORTANT: Save this token now. You will not be able to see it again.");
        return true;
    }

    public static async Task<bool> RunRevokeApiTokenAsync(IServiceProvider services, string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Error: Please provide the user's email address and token name. Usage: REVOKE-API-TOKEN <email> <token-name>");
            return false;
        }

        string email = args[1];
        string tokenName = args[2];
        IRepository repository = services.GetRequiredService<IRepository>();

        User? user = await repository.QueryUnfiltered<User>()
            .FirstOrDefaultAsync(u => EF.Functions.Like(u.Email, email));

        if (user == null)
        {
            Console.WriteLine($"Error: User with email '{email}' not found.");
            return false;
        }

        IApiTokenService tokenService = services.GetRequiredService<IApiTokenService>();
        IEnumerable<ApiToken> tokens = await tokenService.ListTokensAsync(user.Id);

        ApiToken? targetToken = tokens.FirstOrDefault(t => string.Equals(t.Name, tokenName, StringComparison.OrdinalIgnoreCase));

        if (targetToken == null)
        {
            Console.WriteLine($"Error: Token with name '{tokenName}' not found for user '{email}'.");
            return false;
        }

        bool success = await tokenService.RevokeTokenAsync(targetToken.Id, user.Id);

        if (success)
        {
            Console.WriteLine($"Successfully revoked API token '{tokenName}' for user '{email}'.");
            return true;
        }

        Console.WriteLine($"Error: Failed to revoke API token '{tokenName}' (it may already be revoked).");
        return false;
    }

    public static async Task<bool> RunMergeUsersAsync(IServiceProvider services, string[] args)
    {
        IDebugOperationsService debugOps = services.GetRequiredService<IDebugOperationsService>();

        if (args.Length < 3)
        {
            List<MergeUserDto> users = await debugOps.GetAllUsersWithGroupsAsync();
            Console.WriteLine($"{"Email / Name",-40} {"Group",-30} Members");
            Console.WriteLine(new string('-', 80));
            foreach (MergeUserDto u in users)
            {
                Console.WriteLine($"{u.Name,-40} {u.GroupName,-30} {u.GroupMemberCount}");
                Console.WriteLine($"  ID: {u.Id}");
            }

            Console.WriteLine();
            Console.WriteLine("Usage: MERGE-USERS <email1> <email2> [--confirm]");
            Console.WriteLine("  email1 = target (keeps data), email2 = source (deleted)");
            return true;
        }

        string email1 = args[1];
        string email2 = args[2];
        bool confirmed = args.Length >= 4 && args[3].Equals("--confirm", StringComparison.OrdinalIgnoreCase);

        IRepository repository = services.GetRequiredService<IRepository>();

        User? user1 = await repository.QueryUnfiltered<User>()
            .FirstOrDefaultAsync(u => EF.Functions.Like(u.Email, email1));
        User? user2 = await repository.QueryUnfiltered<User>()
            .FirstOrDefaultAsync(u => EF.Functions.Like(u.Email, email2));

        if (user1 == null)
        {
            Console.WriteLine($"Error: User '{email1}' not found.");
            return false;
        }

        if (user2 == null)
        {
            Console.WriteLine($"Error: User '{email2}' not found.");
            return false;
        }

        Console.WriteLine($"TARGET (keeps data): {user1.Email} (ID: {user1.Id})");
        Console.WriteLine($"SOURCE (will be deleted): {user2.Email} (ID: {user2.Id})");

        if (!confirmed)
        {
            Console.WriteLine();
            Console.WriteLine("Dry-run complete. Re-run with --confirm to perform the merge.");
            return true;
        }

        MergeAccountsResult result = await debugOps.MergeAccountsAsync(user1.Id, user2.Id);

        if (!result.Success)
        {
            Console.WriteLine($"Error: {result.Error ?? "An error occurred during merge."}");
            return false;
        }

        Console.WriteLine(result.Message ?? "Merge completed successfully.");
        return true;
    }
}