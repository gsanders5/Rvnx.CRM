using Microsoft.OpenApi;
using Rvnx.CRM.API.Authentication;
using Rvnx.CRM.API.OpenApi;
using Rvnx.CRM.API.Services;
using Rvnx.CRM.Core;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Infrastructure;
using System.Text.Json.Serialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums as strings ("Annual", "Forward") instead of integers in both
        // requests and responses. This makes Swagger bodies self-documenting.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddCoreServices();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<ICurrentUserService, ApiTokenCurrentUserService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(ApiTokenAuthenticationOptions.DefaultScheme)
    .AddScheme<ApiTokenAuthenticationOptions, ApiTokenAuthenticationHandler>(ApiTokenAuthenticationOptions.DefaultScheme, options => { });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Rvnx.CRM.API",
        Version = "v1",
        Description = "Personal CRM API. All endpoints require Bearer token authentication. All IDs are GUIDs. "
                    + "Child resources (notes, facts, addresses, contact methods, significant dates) require entityId (parent contact GUID) "
                    + "in their request bodies — they are always scoped to a Person. "
                    + "Relationships require entityId plus entityType (enum: Person or Company)."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "API Token Authorization header using the Bearer scheme. Example: \"Authorization: Bearer crm_xxxxxxxxxxxxxxx\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });

    c.OperationFilter<AllowAnonymousOperationFilter>();

    string xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

WebApplication app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                       | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Rvnx.CRM.API v1"));

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();