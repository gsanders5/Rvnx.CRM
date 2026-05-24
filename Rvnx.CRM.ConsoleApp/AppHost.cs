using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rvnx.CRM.Core;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Infrastructure;

namespace Rvnx.CRM.ConsoleApp;

internal static class AppHost
{
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
}
