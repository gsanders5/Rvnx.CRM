using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Infrastructure.Services;

namespace Rvnx.CRM.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CRMDbContext>(options =>
        {
            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseSqlite(connectionString);
        });

        services.AddMemoryCache();

        services.AddScoped<IRepository, Repository>();

        // 🛡️ Sentinel: Enforce a strict timeout on HttpClient to prevent DoS attacks from hanging external image servers
        services.AddHttpClient<IVCardService, VCardService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // Connection details (base URL + API key) come from the current group's database row
        // and are applied per request inside ImmichService, not on the shared client.
        services.AddHttpClient<IImmichService, ImmichService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddScoped<IImmichSettingsService, ImmichSettingsService>();

        services.AddScoped<IUserSynchronizationService, UserSynchronizationService>();

        services.AddScoped<IContactImportService, ContactImportService>();

        services.AddScoped<ICsvImportService, CsvImportService>();

        services.AddScoped<IContactExportService, ContactExportService>();

        services.AddScoped<ICsvExportService, CsvExportService>();

        services.AddScoped<IMergeService, MergeService>();

        services.AddScoped<IDebugDataService, DebugDataService>();

        services.AddScoped<IDebugOperationsService, DebugOperationsService>();

        services.AddScoped<IContactMethodService, ContactMethodService>();

        services.AddScoped<IFactService, FactService>();

        services.AddScoped<IThumbnailService, ThumbnailService>();

        services.AddScoped<IPetService, PetService>();

        services.AddScoped<IActivityService, ActivityService>();

        services.AddScoped<IAddressService, AddressService>();

        services.AddScoped<IContactTaskService, ContactTaskService>();

        services.AddScoped<INoteService, NoteService>();

        services.AddScoped<ISignificantDateService, SignificantDateService>();

        services.AddScoped<IReminderNotificationService, ReminderNotificationService>();

        services.AddScoped<IApiTokenService, ApiTokenService>();

        services.AddScoped<ICalendarFeedService, CalendarFeedService>();

        return services;
    }

    private static readonly Action<ILogger, Exception?> LogMigrationError =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1, nameof(LogMigrationError)),
            "An error occurred migrating the database");

    public static async Task<bool> ApplyDatabaseMigrationsAsync(this IServiceProvider provider, ILogger logger)
    {
        try
        {
            using IServiceScope scope = provider.CreateScope();
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
