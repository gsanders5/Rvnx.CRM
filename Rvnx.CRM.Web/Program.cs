using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Infrastructure;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Web.Services;
using System.Security.Claims;

namespace Rvnx.CRM.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

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
                    options.Scope.Clear();
                    foreach (var scope in (authConfig["Scopes"] ?? "openid profile email").Split(' '))
                    {
                        options.Scope.Add(scope);
                    }

                    options.Events = new OpenIdConnectEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var dbContext = context.HttpContext.RequestServices.GetRequiredService<CRMDbContext>();
                            var principal = context.Principal;

                            var subject = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                       ?? principal.FindFirst("sub")?.Value;

                            var email = principal.FindFirst(ClaimTypes.Email)?.Value
                                     ?? principal.FindFirst("email")?.Value;

                            var name = principal.FindFirst(ClaimTypes.Name)?.Value
                                    ?? principal.FindFirst("name")?.Value;

                            if (!string.IsNullOrEmpty(subject))
                            {
                                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.SubjectId == subject);
                                if (user == null)
                                {
                                    user = new User
                                    {
                                        SubjectId = subject,
                                        Email = email ?? "unknown@example.com",
                                        DisplayName = name ?? email,
                                        CreatedBy = "System",
                                        LastChangedBy = "System",
                                        // UserId will be set by DB context if null, but here we are bypassing context logic or using it?
                                        // Context logic relies on CurrentUserService.
                                        // But CurrentUserService relies on HttpContext which is being built.
                                        // The user is not yet fully authenticated in HttpContext.User.
                                        // So CurrentUserService.UserId might be null or System.
                                        // So we should let Context handle it or set it manually?
                                        // If we set UserId to "System", it means "System" created this User record. Which is fine.
                                        UserId = "System"
                                    };
                                    dbContext.Users.Add(user);
                                    await dbContext.SaveChangesAsync();
                                }

                                // Map external subject to internal User Id (Guid)
                                var identity = (ClaimsIdentity)principal.Identity;
                                var nameIdClaim = identity.FindFirst(ClaimTypes.NameIdentifier);
                                if (nameIdClaim != null)
                                {
                                    identity.RemoveClaim(nameIdClaim);
                                }
                                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));

                                // Ensure Name claim is set for display
                                if (!identity.HasClaim(c => c.Type == ClaimTypes.Name) && !string.IsNullOrEmpty(user.DisplayName))
                                {
                                     identity.AddClaim(new Claim(ClaimTypes.Name, user.DisplayName));
                                }
                            }
                        }
                    };
                });
            }

            builder.Services.AddControllersWithViews(options =>
            {
                if (authEnabled)
                {
                    var policy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                    options.Filters.Add(new AuthorizeFilter(policy));
                }
            });

            // Add Infrastructure services
            builder.Services.AddInfrastructure(builder.Configuration);

            var app = builder.Build();

            // Ensure database is created (Development only for this task)
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

            // Configure the HTTP request pipeline.
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
