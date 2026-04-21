using Microsoft.Extensions.DependencyInjection;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Services;

namespace Rvnx.CRM.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddScoped<IContactManagementService, ContactManagementService>();

        services.AddScoped<IContactReadService, ContactReadService>();

        services.AddScoped<ISelfContactService, SelfContactService>();

        services.AddScoped<IDashboardService, DashboardService>();

        services.AddScoped<IRelationshipService, RelationshipService>();

        services.AddScoped<IRelationshipSuggestionService, RelationshipSuggestionService>();

        services.AddScoped<IEntityService, EntityService>();

        services.AddScoped<IFileValidationService, FileValidationService>();

        services.AddScoped<ILabelService, LabelService>();

        services.AddScoped<IAttachmentService, AttachmentService>();

        services.AddScoped<IFavoriteService, FavoriteService>();

        return services;
    }
}
