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
    // The database activity timestamp is the authoritative 15-minute idle
    // clock. The longer bounded cookie must not expel a genuinely active shift.
    options.ExpireTimeSpan = TimeSpan.FromHours(
        Math.Clamp(builder.Configuration.GetValue("Session:CookieLifetimeHours", 16), 8, 24));
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
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
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, StaffClaimsPrincipalFactory>();
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
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    var mustChangePassword = context.User.HasClaim(
        StaffClaimsPrincipalFactory.MustChangePasswordClaimType, bool.TrueString);
    var permittedPath = context.Request.Path.StartsWithSegments("/Account/Manage/ChangePassword") ||
                        context.Request.Path.StartsWithSegments("/Account/Logout") ||
                        context.Request.Path.StartsWithSegments("/_framework") ||
                        context.Request.Path.StartsWithSegments("/_blazor") ||
                        context.Request.Path.StartsWithSegments("/css") ||
                        context.Request.Path.StartsWithSegments("/js") ||
                        context.Request.Path.StartsWithSegments("/lib") ||
                        context.Request.Path.StartsWithSegments("/images") ||
                        Path.HasExtension(context.Request.Path.Value);
    if (context.User.Identity?.IsAuthenticated == true && mustChangePassword && !permittedPath)
    {
        context.Response.Redirect("/Account/Manage/ChangePassword?forced=true");
        return;
    }

    await next();
});
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapAdditionalIdentityEndpoints();
app.MapPost("/security/session/register/{instanceId}", async (
    string instanceId,
    HttpContext context,
    Roms.Web.Components.Account.StaffSessionService sessions) =>
{
    return await sessions.RegisterApplicationInstanceAsync(context.User, instanceId) switch
    {
        Roms.Web.Components.Account.ApplicationInstanceRegistration.Accepted => Results.NoContent(),
        Roms.Web.Components.Account.ApplicationInstanceRegistration.ReplayDetected => Results.Conflict(),
        _ => Results.Unauthorized()
    };
}).RequireAuthorization();
app.MapPost("/security/session/touch/{instanceId}", async (
    string instanceId,
    HttpContext context,
    Roms.Web.Components.Account.StaffSessionService sessions) =>
{
    return await sessions.TouchAsync(context.User, instanceId)
        ? Results.NoContent()
        : Results.Conflict();
}).RequireAuthorization();
app.MapHub<OrderHub>("/hubs/orders");
app.MapHealthChecks("/health");
app.MapAttendanceExport();

await DbInitializer.InitializeAsync(app.Services, app.Environment);
await app.RunAsync();

public partial class Program;
