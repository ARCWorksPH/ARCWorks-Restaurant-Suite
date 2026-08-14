using Microsoft.Extensions.Options;

namespace Roms.Web.Configuration;

public interface IRestaurantProfile
{
    RestaurantPresentation Current { get; }
}

public sealed record RestaurantPresentation(
    string DisplayName,
    string ShortName,
    string Descriptor,
    string TimeZone,
    string Locale,
    string Currency,
    string SupportMessage,
    string? ContactEmail,
    string? ContactPhone,
    string PrimaryLogo,
    string CompactLogo,
    string ProductLogo,
    string DesktopBackground,
    string DesktopBackgroundWebp,
    string MobileBackground,
    string MobileBackgroundWebp,
    string DefaultPortrait,
    string AccentGold,
    string AccentCool);

/// <summary>
/// Resolves optional presentation files to neutral local fallbacks. Unsafe
/// paths are rejected earlier by startup validation; a missing optional file
/// must never trigger a remote request or break the sign-in surface.
/// </summary>
public sealed class RestaurantProfile : IRestaurantProfile
{
    private const string LogoFallback = "/images/branding/restaurant-logo-placeholder.svg";
    private const string ProductFallback = "/images/branding/arcworks-mark.png";
    private const string BackgroundFallback = "/images/branding/login-background-placeholder.svg";
    private readonly IWebHostEnvironment environment;
    private readonly RestaurantOptions options;

    public RestaurantProfile(IOptions<RestaurantOptions> options, IWebHostEnvironment environment)
    {
        this.options = options.Value;
        this.environment = environment;
        Current = Build();
    }

    public RestaurantPresentation Current { get; }

    private RestaurantPresentation Build()
    {
        var assets = options.Assets;
        return new(
            options.DisplayName.Trim(),
            options.ShortName.Trim(),
            options.Descriptor.Trim(),
            options.TimeZone,
            options.Locale,
            options.Currency,
            options.SupportMessage.Trim(),
            NullIfWhiteSpace(options.ContactEmail),
            NullIfWhiteSpace(options.ContactPhone),
            Resolve(assets.PrimaryLogo, LogoFallback),
            Resolve(assets.CompactLogo, LogoFallback),
            Resolve(assets.ProductLogo, ProductFallback),
            Resolve(assets.DesktopBackground, BackgroundFallback),
            Resolve(assets.DesktopBackgroundWebp, BackgroundFallback),
            Resolve(assets.MobileBackground, BackgroundFallback),
            Resolve(assets.MobileBackgroundWebp, BackgroundFallback),
            Resolve(assets.DefaultPortrait, LogoFallback),
            options.Theme.AccentGold,
            options.Theme.AccentCool);
    }

    private string Resolve(string configured, string fallback)
    {
        if (!RestaurantOptions.IsSafeLocalAssetPath(configured)) return fallback;
        var relative = configured.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var candidate = Path.GetFullPath(Path.Combine(environment.WebRootPath, relative));
        var root = Path.GetFullPath(environment.WebRootPath) + Path.DirectorySeparatorChar;
        return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(candidate)
            ? configured
            : fallback;
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
