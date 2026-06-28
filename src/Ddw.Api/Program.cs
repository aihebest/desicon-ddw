using Ddw.Api.Data;
using Ddw.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Passwordless Azure SQL: the connection string (from Key Vault via the App
// Service) uses "Authentication=Active Directory Managed Identity".
var conn = builder.Configuration.GetConnectionString("Default");

builder.Services.AddDbContext<DdwDbContext>(o =>
    o.UseSqlServer(conn ?? "Server=tcp:unconfigured;Database=ddw;",
        sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new()
{
    Title = "Desicon Enterprise Communication & Alert Platform",
    Version = "v1",
    Description = "Announcements, role-based targeting, acknowledgments and analytics."
}));
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Create the schema on first run (dev convenience; switch to EF migrations for prod).
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<DdwDbContext>();
        db.Database.EnsureCreated();
        logger.LogInformation("Database schema ready.");
    }
    catch (Exception ex)
    {
        // Don't crash the app if the DB isn't reachable yet — /health stays up.
        logger.LogError(ex, "Database not ready at startup; will serve once reachable.");
    }
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DDW API v1"));
app.UseCors();

app.MapDdwApi();

app.Run();
