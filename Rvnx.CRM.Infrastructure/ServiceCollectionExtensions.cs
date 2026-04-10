using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddScoped<IUserSynchronizationService, UserSynchronizationService>();

        services.AddScoped<IContactImportService, ContactImportService>();

        services.AddScoped<IContactExportService, ContactExportService>();

        services.AddScoped<IMergeService, MergeService>();

        services.AddScoped<IDebugDataService, DebugDataService>();

        services.AddScoped<IDebugOperationsService, DebugOperationsService>();

        services.AddScoped<IContactMethodService, ContactMethodService>();

        services.AddScoped<IFactService, FactService>();

        services.AddScoped<IThumbnailService, ThumbnailService>();

        services.AddScoped<IPetService, PetService>();

        services.AddScoped<IActivityService, ActivityService>();

        services.AddScoped<IAddressService, AddressService>();

        services.AddScoped<INoteService, NoteService>();

        services.AddScoped<ISignificantDateService, SignificantDateService>();

        services.AddScoped<IReminderNotificationService, ReminderNotificationService>();

        services.AddScoped<IApiTokenService, ApiTokenService>();

        return services;
    }
}