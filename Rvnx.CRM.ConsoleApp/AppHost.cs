using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rvnx.CRM.Core;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Infrastructure;
using Rvnx.CRM.Infrastructure.Data;

namespace Rvnx.CRM.ConsoleApp;

internal static class AppHost
{
    private static readonly Action<ILogger, Exception?> LogMigrationError =
        LoggerMessage.Define(LogLevel.Error, new EventId(1), "An error occurred migrating the database");

    public static IHost Build()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

        builder.Services.AddScoped<ICurrentUserService, ConsoleUserService>();
        builder.Services.AddCoreServices();
        builder.Services.AddInfrastructure(builder.Configuration);

        return builder.Build();
    }

    public static async Task<bool> MigrateDatabaseAsync(IHost host, ILogger logger)
    {
        try
        {
            using IServiceScope scope = host.Services.CreateScope();
            CRMDbContext context = scope.ServiceProvider.GetRequiredService<CRMDbContext>();
            await context.Database.MigrateAsync();
            return true;
        }
        catch (Exception ex)
        {
            LogMigrationError(logger, ex);
            return false;
        }
    }
}