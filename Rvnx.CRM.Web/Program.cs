using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Infrastructure;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Web.Services;

namespace Rvnx.CRM.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddScoped<IUserSynchronizationService, UserSynchronizationService>();

            var authConfig = builder.Configuration.GetSection("Authentication");
            bool authEnabled = authConfig.GetValue<bool>("Enabled");

            if (authEnabled)
            {
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
                    var scopes = (authConfig["Scopes"] ?? "openid profile email offline_access").Split(' ');
                    foreach (var scope in scopes)
                    {
                        options.Scope.Add(scope);
                    }

                    options.Events = new OpenIdConnectEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var userSyncService = context.HttpContext.RequestServices.GetRequiredService<IUserSynchronizationService>();
                            if (context.Principal != null)
                            {
                                await userSyncService.SyncUserAsync(context.Principal);
                            }
                        }
                    };
                });
            }

            builder.Services.AddControllersWithViews();

            builder.Services.AddInfrastructure(builder.Configuration);

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<CRMDbContext>();
                    context.Database.EnsureCreated();
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred creating the DB.");
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
