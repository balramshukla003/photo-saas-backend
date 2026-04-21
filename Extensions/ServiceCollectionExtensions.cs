using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PhotoPrint.API.Data;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Extensions;

public static class ServiceCollectionExtensions
{
    // ── Database — DB First, no migrations ────────────────────────────────
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration config)
    {
        var connStr = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");

        services.AddDbContext<PhotoPrintDbContext>(opt =>
            opt.UseMySql(
                connStr,
                ServerVersion.AutoDetect(connStr),
                mySqlOpt =>
                {
                    mySqlOpt.CommandTimeout(30);
                    mySqlOpt.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                }
            )
        );

        return services;
    }

    // ── JWT Authentication ─────────────────────────────────────────────────
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration config)
    {
        var secret = config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is missing.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = key,
                    ValidateIssuer           = true,
                    ValidIssuer              = config["Jwt:Issuer"],
                    ValidateAudience         = true,
                    ValidAudience            = config["Jwt:Audience"],
                    ValidateLifetime         = true,
                    ClockSkew                = TimeSpan.Zero,
                };

                opt.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        ctx.Response.Headers["WWW-Authenticate"] =
                            "Bearer error=\"invalid_token\"";
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }

    // ── CORS ───────────────────────────────────────────────────────────────
    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration config)
    {
        var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? ["http://localhost:5173", "http://localhost:3000"];

        services.AddCors(opt =>
            opt.AddPolicy("FrontendPolicy", policy =>
                policy
                    .WithOrigins(origins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()));

        return services;
    }

    // ── Application Services ───────────────────────────────────────────────
    public static IServiceCollection AddAppServices(
    this IServiceCollection services,
    IConfiguration config)
    {
        // Python rembg client (keep or remove if not using)
        services.AddHttpClient("PythonService", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        // ── remove.bg API client ──────────────────────────────────────────
        services.AddHttpClient("RemoveBgService", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
            client.BaseAddress = new Uri("https://api.remove.bg");
        });

        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPhotoService, PhotoService>();

        return services;
    }

    // ── Swagger ────────────────────────────────────────────────────────────
    public static IServiceCollection AddSwaggerDocs(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(opt =>
        {
            opt.SwaggerDoc("v1", new OpenApiInfo
            {
                Title       = "PhotoPrint API",
                Version     = "v1",
                Description = "Passport photo processing SaaS — DB First approach",
            });

            // JWT auth button in Swagger UI
            opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name         = "Authorization",
                Type         = SecuritySchemeType.Http,
                Scheme       = "bearer",
                BearerFormat = "JWT",
                In           = ParameterLocation.Header,
                Description  = "Enter JWT token (without 'Bearer' prefix)",
            });

            opt.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                            { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
