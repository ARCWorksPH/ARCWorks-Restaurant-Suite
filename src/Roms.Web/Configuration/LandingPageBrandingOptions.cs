namespace Roms.Web.Configuration;

/// <summary>
/// Per-restaurant presentation values for the unauthenticated landing page.
/// Values deliberately contain no credentials, tenant routing, or authorization
/// state. Local static-asset paths avoid making the sign-in page depend on a
/// third-party image host.
/// </summary>
public sealed class LandingPageBrandingOptions
{
    public const string SectionName = "LandingPageBranding";

    public string RestaurantName { get; init; } = "Your Restaurant";
    public string RestaurantDescriptor { get; init; } = "Restaurant";
    public string RestaurantLogoPath { get; init; } = "/images/branding/restaurant-logo-placeholder.svg";
    public string BackgroundImagePath { get; init; } = "/images/branding/login-background-placeholder.svg";
    public string SupportMessage { get; init; } = "Need access? Contact your restaurant administrator.";

    public static bool IsSafeLocalAssetPath(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.StartsWith("/images/", StringComparison.Ordinal) &&
        !value.Contains("..", StringComparison.Ordinal) &&
        !value.Contains('?', StringComparison.Ordinal) &&
        !value.Contains('#', StringComparison.Ordinal);
}
