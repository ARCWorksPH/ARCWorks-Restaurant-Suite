using Microsoft.Extensions.Options;

namespace Roms.Web.Configuration;

/// <summary>
/// Replaceable restaurant identity and presentation settings. This section is
/// deliberately separate from tenant identity, authentication, permissions,
/// timers, and operational history.
/// </summary>
public sealed class RestaurantOptions
{
    public const string SectionName = "Restaurant";
    public const string ManilaTimeZone = "Asia/Manila";

    public string DisplayName { get; init; } = "Restaurant";
    public string ShortName { get; init; } = "Restaurant";
    public string Descriptor { get; init; } = "Restaurant";
    public string TimeZone { get; init; } = ManilaTimeZone;
    public string Locale { get; init; } = "en-PH";
    public string Currency { get; init; } = "PHP";
    public string SupportMessage { get; init; } = "Need access? Contact your restaurant administrator.";
    public string? ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
    public RestaurantAssetOptions Assets { get; init; } = new();
    public RestaurantThemeOptions Theme { get; init; } = new();

    public static bool IsSafeLocalAssetPath(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.StartsWith("/images/", StringComparison.Ordinal) &&
        !value.StartsWith("//", StringComparison.Ordinal) &&
        !value.Contains("..", StringComparison.Ordinal) &&
        !value.Contains('?', StringComparison.Ordinal) &&
        !value.Contains('#', StringComparison.Ordinal) &&
        Uri.TryCreate(value, UriKind.Relative, out _);
}

public sealed class RestaurantAssetOptions
{
    public string PrimaryLogo { get; init; } = "/images/branding/restaurant-logo-placeholder.svg";
    public string CompactLogo { get; init; } = "/images/branding/restaurant-logo-placeholder.svg";
    public string ProductLogo { get; init; } = "/images/branding/arcworks-mark.png";
    public string DesktopBackground { get; init; } = "/images/branding/login-background-placeholder.svg";
    public string DesktopBackgroundWebp { get; init; } = "/images/branding/login-background-placeholder.svg";
    public string MobileBackground { get; init; } = "/images/branding/login-background-placeholder.svg";
    public string MobileBackgroundWebp { get; init; } = "/images/branding/login-background-placeholder.svg";
    public string DefaultPortrait { get; init; } = "/images/branding/restaurant-logo-placeholder.svg";
}

public sealed class RestaurantThemeOptions
{
    public string AccentGold { get; init; } = "#C9983A";
    public string AccentCool { get; init; } = "#20C7D3";
}

public sealed class RestaurantOptionsValidator : IValidateOptions<RestaurantOptions>
{
    public ValidateOptionsResult Validate(string? name, RestaurantOptions options)
    {
        var errors = new List<string>();
        RequireLength(options.DisplayName, 2, 100, "Restaurant:DisplayName", errors);
        RequireLength(options.ShortName, 2, 40, "Restaurant:ShortName", errors);
        OptionalLength(options.Descriptor, 80, "Restaurant:Descriptor", errors);
        RequireLength(options.SupportMessage, 2, 240, "Restaurant:SupportMessage", errors);
        OptionalLength(options.ContactEmail, 200, "Restaurant:ContactEmail", errors);
        OptionalLength(options.ContactPhone, 40, "Restaurant:ContactPhone", errors);

        if (!string.Equals(options.TimeZone, RestaurantOptions.ManilaTimeZone, StringComparison.Ordinal))
            errors.Add($"Restaurant:TimeZone must be {RestaurantOptions.ManilaTimeZone} for this deployment.");
        else
        {
            try { _ = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZone); }
            catch (TimeZoneNotFoundException) { errors.Add("Restaurant:TimeZone is not available on this server."); }
            catch (InvalidTimeZoneException) { errors.Add("Restaurant:TimeZone is invalid on this server."); }
        }

        if (!string.Equals(options.Locale, "en-PH", StringComparison.OrdinalIgnoreCase))
            errors.Add("Restaurant:Locale must be en-PH for this deployment.");
        if (!string.Equals(options.Currency, "PHP", StringComparison.OrdinalIgnoreCase))
            errors.Add("Restaurant:Currency must be PHP for this deployment.");

        ValidateAsset(options.Assets.PrimaryLogo, "Restaurant:Assets:PrimaryLogo", errors);
        ValidateAsset(options.Assets.CompactLogo, "Restaurant:Assets:CompactLogo", errors);
        ValidateAsset(options.Assets.ProductLogo, "Restaurant:Assets:ProductLogo", errors);
        ValidateAsset(options.Assets.DesktopBackground, "Restaurant:Assets:DesktopBackground", errors);
        ValidateAsset(options.Assets.DesktopBackgroundWebp, "Restaurant:Assets:DesktopBackgroundWebp", errors);
        ValidateAsset(options.Assets.MobileBackground, "Restaurant:Assets:MobileBackground", errors);
        ValidateAsset(options.Assets.MobileBackgroundWebp, "Restaurant:Assets:MobileBackgroundWebp", errors);
        ValidateAsset(options.Assets.DefaultPortrait, "Restaurant:Assets:DefaultPortrait", errors);
        ValidateColor(options.Theme.AccentGold, "Restaurant:Theme:AccentGold", errors);
        ValidateColor(options.Theme.AccentCool, "Restaurant:Theme:AccentCool", errors);

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }

    private static void RequireLength(string? value, int minimum, int maximum, string key, List<string> errors)
    {
        var length = value?.Trim().Length ?? 0;
        if (length < minimum || length > maximum)
            errors.Add($"{key} must contain {minimum} to {maximum} characters.");
    }

    private static void OptionalLength(string? value, int maximum, string key, List<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maximum)
            errors.Add($"{key} must contain at most {maximum} characters.");
    }

    private static void ValidateAsset(string? value, string key, List<string> errors)
    {
        if (!RestaurantOptions.IsSafeLocalAssetPath(value))
            errors.Add($"{key} must be a local /images/ asset path without traversal, query, or fragment data.");
    }

    private static void ValidateColor(string? value, string key, List<string> errors)
    {
        if (value is null || value.Length != 7 || value[0] != '#' ||
            !value.AsSpan(1).ToString().All(Uri.IsHexDigit))
            errors.Add($"{key} must be a #RRGGBB color.");
    }
}
