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
using Roms.Web.Ai;
using Roms.Application.Commands;
using Roms.Web.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"] ?? "DataProtection-Keys";
if (!Path.IsPathRooted(dataProtectionKeysPath))
{
    dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, dataProtectionKeysPath);
}

Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    // Keep the legacy application name so existing cookies and key rings remain valid.
    .SetApplicationName("ROMS");

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddOptions<LandingPageBrandingOptions>()
    .Bind(builder.Configuration.GetSection(LandingPageBrandingOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.RestaurantName) && options.RestaurantName.Length <= 120,
        "LandingPageBranding:RestaurantName must contain at most 120 characters.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.RestaurantDescriptor) && options.RestaurantDescriptor.Length <= 120,
        "LandingPageBranding:RestaurantDescriptor must contain at most 120 characters.")
    .Validate(options => LandingPageBrandingOptions.IsSafeLocalAssetPath(options.RestaurantLogoPath),
        "LandingPageBranding:RestaurantLogoPath must be a local /images/ asset path.")
    .Validate(options => LandingPageBrandingOptions.IsSafeLocalAssetPath(options.ProductLogoPath),
        "LandingPageBranding:ProductLogoPath must be a local /images/ asset path.")
    .Validate(options => LandingPageBrandingOptions.IsSafeLocalAssetPath(options.BackgroundImagePath),
        "LandingPageBranding:BackgroundImagePath must be a local /images/ asset path.")
    .Validate(options => LandingPageBrandingOptions.IsSafeLocalAssetPath(options.BackgroundWebpPath),
        "LandingPageBranding:BackgroundWebpPath must be a local /images/ asset path.")
    .Validate(options => LandingPageBrandingOptions.IsSafeLocalAssetPath(options.MobileBackgroundImagePath),
        "LandingPageBranding:MobileBackgroundImagePath must be a local /images/ asset path.")
    .Validate(options => LandingPageBrandingOptions.IsSafeLocalAssetPath(options.MobileBackgroundWebpPath),
        "LandingPageBranding:MobileBackgroundWebpPath must be a local /images/ asset path.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.SupportMessage) && options.SupportMessage.Length <= 240,
        "LandingPageBranding:SupportMessage must contain at most 240 characters.")
    .ValidateOnStart();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // cloudflared runs on this server and forwards the visitor's HTTPS scheme.
    // The middleware's default trust list only accepts loopback proxies.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<StaffSessionService>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(options =>
{
    // The server-side StaffSessionService separately validates actual activity.
    // The cookie remains short-lived as a second boundary for shared devices.
    options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
    options.SlidingExpiration = false;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddAntiforgery(options =>
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest);
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(180);
    options.IncludeSubDomains = false;
    options.Preload = false;
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
    .AddPolicy("KitchenOrAdmin", p => p.RequireRole(RomsRoles.Kitchen, RomsRoles.Admin))
    .AddPolicy("ManagerOrAdmin", p => p.RequireRole(RomsRoles.Manager, RomsRoles.Admin))
    .AddPolicy("KitchenManagerOrAdmin", p => p.RequireRole(RomsRoles.Kitchen, RomsRoles.Manager, RomsRoles.Admin));

builder.Services.AddSignalR(o => o.MaximumReceiveMessageSize = 32 * 1024);
builder.Services.AddSingleton<OrderEventBus>();
builder.Services.AddScoped<IOrderEventPublisher, SignalROrderEventPublisher>();
builder.Services.AddHealthChecks();
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection("Seed"));
// AI is a future-version feature. While the hold is active the web process
// does not register an HTTP client and cannot establish an app-to-gateway
// connection, even if a stale Ai:Enabled environment variable is present.
if (AiFeatureGate.IsEnabled(builder.Configuration))
{
    var commandGatewayBaseUrl = builder.Configuration["Ai:CommandGatewayBaseUrl"];
    if (string.IsNullOrWhiteSpace(commandGatewayBaseUrl))
    {
        throw new InvalidOperationException(
            "Ai:CommandGatewayBaseUrl is required only when the AI hold is released.");
    }

    builder.Services.AddHttpClient<ICommandGatewayClient, CommandGatewayClient>(client =>
    {
        client.BaseAddress = new Uri(commandGatewayBaseUrl, UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(50);
    });
}

var app = builder.Build();

app.UseForwardedHeaders();
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "SAMEORIGIN";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers["Content-Security-Policy"] =
            "frame-ancestors 'self'; base-uri 'self'; object-src 'none'";
        return Task.CompletedTask;
    });
    await next();
});

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
