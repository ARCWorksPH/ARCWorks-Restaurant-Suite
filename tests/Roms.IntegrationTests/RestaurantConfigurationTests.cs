using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Roms.Application;
using Roms.Web.Configuration;

namespace Roms.IntegrationTests;

public sealed class RestaurantConfigurationTests
{
    [Fact]
    public void Validator_accepts_the_approved_local_profile()
    {
        var result = new RestaurantOptionsValidator().Validate(null, ValidOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validator_rejects_remote_assets_and_non_restaurant_timezone()
    {
        var options = ValidOptions(
            timeZone: "UTC",
            assets: new RestaurantAssetOptions { PrimaryLogo = "https://example.invalid/logo.svg" });

        var result = new RestaurantOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, message => message.Contains("Restaurant:TimeZone", StringComparison.Ordinal));
        Assert.Contains(result.Failures, message => message.Contains("Restaurant:Assets:PrimaryLogo", StringComparison.Ordinal));
    }

    [Fact]
    public void Profile_uses_replacement_asset_when_present_and_local_fallback_when_missing()
    {
        var webRoot = Path.Combine(Path.GetTempPath(), $"roms-branding-{Guid.NewGuid():N}");
        var branding = Path.Combine(webRoot, "images", "branding");
        Directory.CreateDirectory(branding);
        File.WriteAllText(Path.Combine(branding, "custom.svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\"/>");

        try
        {
            var options = ValidOptions(assets: new RestaurantAssetOptions
            {
                PrimaryLogo = "/images/branding/custom.svg",
                CompactLogo = "/images/branding/missing.svg"
            });
            var profile = new RestaurantProfile(
                Options.Create(options),
                new TestWebHostEnvironment(webRoot));

            Assert.Equal("/images/branding/custom.svg", profile.Current.PrimaryLogo);
            Assert.Equal("/images/branding/restaurant-logo-placeholder.svg", profile.Current.CompactLogo);
            Assert.Equal("Chef Doy's Gourmet Restaurant", profile.Current.DisplayName);
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [Fact]
    public void Restaurant_clock_keeps_UTC_persistence_and_uses_Manila_calendar()
    {
        var utc = new DateTime(2026, 8, 16, 16, 30, 0, DateTimeKind.Utc);
        var clock = new RestaurantClock(
            new FixedClock(utc),
            Options.Create(ValidOptions()));

        Assert.Equal(utc, clock.UtcNow);
        Assert.Equal(new DateTime(2026, 8, 17, 0, 30, 0, DateTimeKind.Unspecified), clock.LocalNow);
        Assert.Equal(new DateOnly(2026, 8, 17), clock.LocalDate);
        Assert.Equal(new DateOnly(2026, 8, 17), clock.StartOfWeek(clock.LocalDate));
        Assert.Equal(utc, clock.ToUtc(new DateTime(2026, 8, 17, 0, 30, 0, DateTimeKind.Unspecified)));
    }

    private static RestaurantOptions ValidOptions(
        string timeZone = RestaurantOptions.ManilaTimeZone,
        RestaurantAssetOptions? assets = null) => new()
    {
        DisplayName = "Chef Doy's Gourmet Restaurant",
        ShortName = "Chef Doy's",
        Descriptor = "GOURMET RESTAURANT",
        TimeZone = timeZone,
        Locale = "en-PH",
        Currency = "PHP",
        SupportMessage = "Contact your restaurant administrator.",
        Assets = assets ?? new RestaurantAssetOptions(),
        Theme = new RestaurantThemeOptions()
    };

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class TestWebHostEnvironment(string webRootPath) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Roms.IntegrationTests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = webRootPath;
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = webRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
