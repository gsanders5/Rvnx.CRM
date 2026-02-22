using Microsoft.Extensions.DependencyInjection;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Services;

namespace Rvnx.CRM.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // Add Contact Management Service
        services.AddScoped<IContactManagementService, ContactManagementService>();

        // Add Contact Read Service
        services.AddScoped<IContactReadService, ContactReadService>();

        // Add Self Contact Service
        services.AddScoped<ISelfContactService, SelfContactService>();

        // Add Dashboard Service
        services.AddScoped<IDashboardService, DashboardService>();

        // Add Relationship Service
        services.AddScoped<IRelationshipService, RelationshipService>();

        // Add Entity Service
        services.AddScoped<IEntityService, EntityService>();

        // Add File Validation Service
        services.AddScoped<IFileValidationService, FileValidationService>();

        return services;
    }
}
