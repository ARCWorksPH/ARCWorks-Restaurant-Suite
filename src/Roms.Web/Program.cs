using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Roms.Application;
using Roms.Infrastructure;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Persistence;
using Roms.Web.Components;
using Roms.Web.Components.Account;
using Roms.Web.Realtime;
using Roms.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"] ?? "DataProtection-Keys";
if (!Path.IsPathRooted(dataProtectionKeysPath))
{
    dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, dataProtectionKeysPath);
}

Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("ROMS");

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // cloudflared runs on this server and forwards the visitor's HTTPS scheme.
    // The middleware's default trust list only accepts loopback proxies.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.SlidingExpiration = false;
});

builder.Services.AddRomsInfrastructure(builder.Configuration);
builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 10;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version2;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<RomsDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("WaiterOrAdmin", p => p.RequireRole(RomsRoles.Waiter, RomsRoles.Admin))
    .AddPolicy("KitchenOrAdmin", p => p.RequireRole(RomsRoles.Kitchen, RomsRoles.Admin));

builder.Services.AddSignalR(o => o.MaximumReceiveMessageSize = 32 * 1024);
builder.Services.AddSingleton<OrderEventBus>();
builder.Services.AddScoped<IOrderEventPublisher, SignalROrderEventPublisher>();
builder.Services.AddHealthChecks();
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection("Seed"));

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment()) app.UseMigrationsEndPoint();
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapAdditionalIdentityEndpoints();
app.MapHub<OrderHub>("/hubs/orders");
app.MapHealthChecks("/health");
app.MapAttendanceExport();

await DbInitializer.InitializeAsync(app.Services, app.Environment);
await app.RunAsync();

public partial class Program;
