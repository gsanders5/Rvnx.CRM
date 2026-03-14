using Microsoft.OpenApi.Models;
using Rvnx.CRM.API.Authentication;
using Rvnx.CRM.API.Services;
using Rvnx.CRM.Core;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register CRM core and infrastructure services
builder.Services.AddCoreServices();
builder.Services.AddInfrastructure(builder.Configuration);

// Add API Token Authentication
builder.Services.AddScoped<ICurrentUserService, ApiTokenCurrentUserService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(ApiTokenAuthenticationOptions.DefaultScheme)
    .AddScheme<ApiTokenAuthenticationOptions, ApiTokenAuthenticationHandler>(ApiTokenAuthenticationOptions.DefaultScheme, options => { });

builder.Services.AddAuthorization();

// Swagger configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Rvnx.CRM.API", Version = "v1" });

    // Define Bearer Auth
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "API Token Authorization header using the Bearer scheme. Example: \"Authorization: Bearer crm_xxxxxxxxxxxxxxx\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

WebApplication app = builder.Build();

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Rvnx.CRM.API v1"));
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();