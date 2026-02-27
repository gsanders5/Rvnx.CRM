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
        // Add DbContext
        services.AddDbContext<CRMDbContext>(options =>
        {
            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseSqlite(connectionString);
        });

        // Add Repository
        services.AddScoped<IRepository, Repository>();

        // Add VCard Service
        services.AddHttpClient<IVCardService, VCardService>();

        // Add User Synchronization Service
        services.AddScoped<IUserSynchronizationService, UserSynchronizationService>();

        // Add Contact Import Service
        services.AddScoped<IContactImportService, ContactImportService>();

        // Add Contact Export Service
        services.AddScoped<IContactExportService, ContactExportService>();

        // Add Debug Data Service
        services.AddScoped<IDebugDataService, DebugDataService>();

        // Add Debug Operations Service
        services.AddScoped<IDebugOperationsService, DebugOperationsService>();

        // Add Contact Method Service
        services.AddScoped<IContactMethodService, ContactMethodService>();

        // Add Fact Service
        services.AddScoped<IFactService, FactService>();

        // Add Pet Service
        services.AddScoped<IPetService, PetService>();

        // Add Note Service
        services.AddScoped<INoteService, NoteService>();

        // Add Reminder Service
        services.AddScoped<IReminderService, ReminderService>();

        // Add Significant Date Service
        services.AddScoped<ISignificantDateService, SignificantDateService>();

        return services;
    }
}
