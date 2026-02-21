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

        return services;
    }
}
