using PhotoPrint.API.Extensions;
using PhotoPrint.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── Service registration (all in extension methods) ───────────────────────
builder.Services
    .AddDatabase(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddCorsPolicy(builder.Configuration)
    .AddAppServices(builder.Configuration)
    .AddSwaggerDocs()
    .AddControllers();

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────
// ORDER IS CRITICAL — do not reorder
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opt =>
    {
        opt.SwaggerEndpoint("/swagger/v1/swagger.json", "PhotoPrint API v1");
        opt.RoutePrefix = "swagger";
    });
}

app.UseCors("FrontendPolicy");

app.UseAuthentication();           // 1. Validate JWT → populate User.Claims
app.UseMiddleware<LicenseMiddleware>(); // 2. Check license claims
app.UseAuthorization();            // 3. [Authorize] attribute enforcement

app.MapControllers();

// ── NOTE: DB First — no app.MigrateDatabase() here ───────────────────────
// Run schema.sql manually before starting the app.
// To re-scaffold after schema changes:
//   dotnet ef dbcontext scaffold "..." Pomelo.EntityFrameworkCore.MySql \
//     --output-dir Data/Scaffolded --context PhotoPrintDbContext \
//     --no-onconfiguring --data-annotations --force

app.Run();
