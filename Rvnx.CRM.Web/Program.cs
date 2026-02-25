using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Rvnx.CRM.Core;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Infrastructure;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Web.Services;
// using FileTypeChecker.Web.Registration; // Method not present in 2.0.0

namespace Rvnx.CRM.Web
{
    public class Program
    {
        private static readonly Action<ILogger, Exception?> LogDbCreationError =
            LoggerMessage.Define(
                LogLevel.Error,
                new EventId(1, nameof(LogDbCreationError)),
                "An error occurred creating the DB.");

        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            // File.TypeChecker.Web 2.0.0 does not expose AddFileTypesValidation.
            // Assuming automatic or no registration required for this version.

            IConfigurationSection authConfig = builder.Configuration.GetSection("Authentication");
            bool authEnabled = authConfig.GetValue<bool>("Enabled");

            if (authEnabled)
            {
                // Validate that required authentication settings are present when enabled
                if (string.IsNullOrWhiteSpace(authConfig["Authority"]) ||
                    string.IsNullOrWhiteSpace(authConfig["ClientId"]) ||
                    string.IsNullOrWhiteSpace(authConfig["ClientSecret"]))
                {
                    throw new InvalidOperationException(
                        "Authentication is enabled but Authority, ClientId, or ClientSecret is missing in configuration. " +
                        "Please provide these values in appsettings.json or via environment variables (e.g., Authentication__ClientSecret).");
                }

                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                })
                .AddOpenIdConnect(options =>
                {
                    options.Authority = authConfig["Authority"];
                    options.ClientId = authConfig["ClientId"];
                    options.ClientSecret = authConfig["ClientSecret"];
                    options.ResponseType = authConfig["ResponseType"] ?? "code";
                    options.SaveTokens = true;
                    options.CallbackPath = authConfig["CallbackPath"] ?? "/signin-oidc";
                    options.RemoteSignOutPath = authConfig["RemoteSignOutPath"] ?? "/signout-oidc";

                    options.MapInboundClaims = true; // Explicitly map standard OIDC claims to .NET ClaimTypes
                    options.GetClaimsFromUserInfoEndpoint = true; // Fetch additional profile info if not in ID token

                    options.Scope.Clear();
                    // Add default scopes including offline_access for Authentik compatibility (Refresh Tokens)
                    string[] scopes = (authConfig["Scopes"] ?? "openid profile email offline_access").Split(' ');
                    foreach (string scope in scopes)
                    {
                        options.Scope.Add(scope);
                    }

                    options.Events = new OpenIdConnectEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            IUserSynchronizationService userSyncService = context.HttpContext.RequestServices.GetRequiredService<IUserSynchronizationService>();
                            if (context.Principal != null)
                            {
                                await userSyncService.SyncUserAsync(context.Principal);
                            }
                        }
                    };
                });
            }

            builder.Services.AddControllersWithViews();

            builder.Services.AddCoreServices();
            builder.Services.AddInfrastructure(builder.Configuration);

            WebApplication app = builder.Build();

            using (IServiceScope scope = app.Services.CreateScope())
            {
                IServiceProvider services = scope.ServiceProvider;
                try
                {
                    CRMDbContext context = services.GetRequiredService<CRMDbContext>();
                    context.Database.EnsureCreated();
                }
                catch (Exception ex)
                {
                    ILogger<Program> logger = services.GetRequiredService<ILogger<Program>>();
                    LogDbCreationError(logger, ex);
                }
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            if (authEnabled)
            {
                app.UseAuthentication();
            }
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
