using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.Models;
using TaskManager.Api.Services;
using TaskManager.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddScoped<AccessTokenService>();
builder.Services.AddScoped<DemoAccountService>();

// Límite conservador por IP para la creación pública de cuentas demo.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("demo-create", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "https://taskflow-azh.pages.dev",
                "https://produccion-wdfb.onrender.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// PostgreSQL: no registrar en los tests de integración
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection")));
}

// La limpieza solo se ejecuta en entornos con base de datos real.
if (!builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddHostedService<DemoCleanupService>();

// Persistencia de claves de autenticación
builder.Services
    .AddDataProtection()
    .SetApplicationName("TaskFlow")
    .PersistKeysToDbContext<ApplicationDbContext>();

// ASP.NET Core Identity
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        // Usuarios
        options.User.RequireUniqueEmail = true;

        // Contraseñas
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        // Bloqueo tras intentos fallidos
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Autenticación mediante tokens Bearer de Identity

builder.Services
    .AddAuthentication(BearerTokenDefaults.AuthenticationScheme)
    .AddBearerToken(
        BearerTokenDefaults.AuthenticationScheme,
        options =>
        {
            options.BearerTokenExpiration =
                TimeSpan.FromMinutes(60);

            options.RefreshTokenExpiration =
                TimeSpan.FromDays(7);
        });


// Autorización
builder.Services.AddAuthorization();

var app = builder.Build();

// Swagger solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware
app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("AngularFrontend");
app.UseRateLimiter();

// 1. Identificar al usuario mediante su access token.
app.UseAuthentication();

// Bloquea tokens de cuentas demo caducadas incluso antes de la limpieza.
app.UseMiddleware<DemoExpirationMiddleware>();

// 2. Comprobar que la familia de sesión sigue activa.
app.UseMiddleware<ActiveSessionMiddleware>();

// 3. Autorizar únicamente sesiones válidas.
app.UseAuthorization();

// Controllers
app.MapControllers();

app.Run();

// Necesario para los tests de integración
public partial class Program { }
