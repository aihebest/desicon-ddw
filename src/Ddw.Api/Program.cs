using Ddw.Api.Components;
using Ddw.Api.Data;
using Ddw.Api.Endpoints;
using Ddw.Api.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// ----- Data (passwordless Azure SQL via managed identity) -----
var conn = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<DdwDbContext>(o =>
    o.UseSqlServer(conn ?? "Server=tcp:unconfigured;Database=ddw;",
        sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

// ----- Behind App Service: trust X-Forwarded-* so OIDC builds https redirect URIs -----
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

// ----- Entra ID: OIDC sign-in for the portal + JWT bearer for the agent API -----
var authBuilder = builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme);
authBuilder.AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));      // portal (cookie/OIDC)
authBuilder.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));       // agent (JWT bearer)

// Entra delivers app roles in the "roles" claim — map it so RequireRole matches.
builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, o =>
    o.TokenValidationParameters.RoleClaimType = "roles");

// Portal is admin-only: require the DDW.Admin app role. The agent (JWT) is unaffected —
// any authenticated employee may poll, so all staff can sign in once assignment is open.
builder.Services.AddAuthorization(options =>
    options.AddPolicy("AdminPortal", p => p.RequireRole("DDW.Admin")));
builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();
builder.Services.AddCascadingAuthenticationState();

// ----- Blazor Server (interactive) portal -----
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// ----- Server-side API client (admin key from Key Vault; never sent to the browser) -----
var adminKey = builder.Configuration["AdminApiKey"] ?? "";
builder.Services.AddHttpClient<DdwApiClient>(c =>
{
    c.BaseAddress = new Uri("http://localhost:8080");
    if (!string.IsNullOrEmpty(adminKey)) c.DefaultRequestHeaders.Add("X-Api-Key", adminKey);
});

// ----- API docs + CORS for the agent -----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new()
{
    Title = "Desicon Enterprise Communication & Alert Platform",
    Version = "v1"
}));
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Create the schema on first run.
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        scope.ServiceProvider.GetRequiredService<DdwDbContext>().Database.EnsureCreated();
        logger.LogInformation("Database schema ready.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database not ready at startup; will serve once reachable.");
    }
}

app.UseForwardedHeaders();
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DDW API v1"));
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// API for the desktop agent (no Entra; admin writes use X-Api-Key).
app.MapDdwApi();
// Microsoft.Identity sign-in/out controller.
app.MapControllers();
// Admin portal (requires Entra sign-in).
app.MapRazorComponents<App>().AddInteractiveServerRenderMode().RequireAuthorization("AdminPortal");

app.Run();
