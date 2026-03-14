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

        services.AddScoped<IRepository, Repository>();

        services.AddHttpClient<IVCardService, VCardService>();

        services.AddScoped<IUserSynchronizationService, UserSynchronizationService>();

        services.AddScoped<IContactImportService, ContactImportService>();

        services.AddScoped<IContactExportService, ContactExportService>();

        services.AddScoped<IMergeService, MergeService>();

        // Add Debug Data Service
        services.AddScoped<IDebugDataService, DebugDataService>();

        // Add Debug Operations Service
        services.AddScoped<IDebugOperationsService, DebugOperationsService>();

        services.AddScoped<IContactMethodService, ContactMethodService>();

        services.AddScoped<IFactService, FactService>();

        services.AddScoped<IPetService, PetService>();

        services.AddScoped<INoteService, NoteService>();

        services.AddScoped<ISignificantDateService, SignificantDateService>();

        services.AddScoped<IReminderNotificationService, ReminderNotificationService>();

        return services;
    }
}