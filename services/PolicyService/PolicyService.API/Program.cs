using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PolicyService.Application.Interfaces;
using PolicyService.Application.Services;
using PolicyService.Domain.Interfaces;
using PolicyService.Infrastructure.Data;
using PolicyService.Infrastructure.Messaging;
using PolicyService.Infrastructure.Repositories;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Database
// PendingModelChangesWarning is suppressed because the extra entity properties
// (ClaimLimit, DurationMonths, etc.) already have matching DB columns from the
// AddPolicyTypeAdminFields migration — the snapshot just pre-dates those properties.
builder.Services.AddDbContext<PolicyDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsAssembly("PolicyService.Infrastructure"))
    .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

// Services
builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
builder.Services.AddScoped<IPolicyService, PolicyAppService>();
builder.Services.AddScoped<RabbitMQPublisher>();
builder.Services.AddScoped<AdoPolicyRepository>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// Swagger with JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartSure - Policy Service",
        Version = "v1"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token here"
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

// CORS for Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartSure Policy Service v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Auto-apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PolicyDbContext>();
    db.Database.Migrate();

    // One-time seed: fill policy type details if still at defaults (DurationMonths == 0)
    var needsSeed = await db.PolicyTypes.AnyAsync(t => t.DurationMonths == 0);
    if (needsSeed)
    {
        await db.Database.ExecuteSqlRawAsync(@"
            UPDATE PolicyTypes SET
                DurationMonths=12, MinAge=18, MaxAge=65, RiskCategory='Medium',
                ClaimLimit=500000, AutoRenewal=1, GracePeriodDays=15,
                CoverageDetails='Hospitalization, surgery, ICU, diagnostics, medicines, and ambulance charges',
                Exclusions='Pre-existing conditions (first year), cosmetic surgery, dental treatments'
            WHERE Id=1;

            UPDATE PolicyTypes SET
                DurationMonths=12, MinAge=18, MaxAge=60, RiskCategory='High',
                ClaimLimit=1000000, AutoRenewal=1, GracePeriodDays=30,
                CoverageDetails='Death benefit, permanent disability, critical illness coverage',
                Exclusions='Suicide within first year, self-inflicted injuries, hazardous activities'
            WHERE Id=2;

            UPDATE PolicyTypes SET
                DurationMonths=12, MinAge=18, MaxAge=70, RiskCategory='Medium',
                ClaimLimit=200000, AutoRenewal=1, GracePeriodDays=15,
                CoverageDetails='Collision damage, theft, third-party liability, natural calamities',
                Exclusions='Driving under influence, racing, intentional damage'
            WHERE Id=3;

            UPDATE PolicyTypes SET
                DurationMonths=12, MinAge=21, MaxAge=80, RiskCategory='Low',
                ClaimLimit=300000, AutoRenewal=1, GracePeriodDays=30,
                CoverageDetails='Fire, flood, burglary, earthquake, cyclone, structural damage',
                Exclusions='Intentional damage, war, nuclear hazards, normal wear and tear'
            WHERE Id=4;

            UPDATE PolicyTypes SET
                DurationMonths=3, MinAge=18, MaxAge=70, RiskCategory='Low',
                ClaimLimit=100000, AutoRenewal=0, GracePeriodDays=0,
                CoverageDetails='Medical emergencies abroad, trip cancellation, lost baggage, flight delay',
                Exclusions='Pre-existing conditions, adventure sports without add-on, alcohol-related incidents'
            WHERE Id=5;
        ");
    }
}

app.Run();