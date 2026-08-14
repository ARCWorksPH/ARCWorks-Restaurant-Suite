using Microsoft.Extensions.Options;
using Roms.Application;

namespace Roms.Web.Configuration;

public sealed class RestaurantClock : IRestaurantClock
{
    private readonly IClock clock;

    public RestaurantClock(IClock clock, IOptions<RestaurantOptions> options)
    {
        this.clock = clock;
        TimeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);
    }

    public DateTime UtcNow => EnsureUtc(clock.UtcNow);
    public DateTime LocalNow => ToLocal(UtcNow);
    public DateOnly LocalDate => DateOnly.FromDateTime(LocalNow);
    public TimeZoneInfo TimeZone { get; }

    public DateTime ToLocal(DateTime utcInstant) =>
        TimeZoneInfo.ConvertTimeFromUtc(EnsureUtc(utcInstant), TimeZone);

    public DateTime ToUtc(DateTime localRestaurantTime)
    {
        if (localRestaurantTime.Kind == DateTimeKind.Local)
            throw new ArgumentException("Restaurant-local time must not use the server's Local DateTime kind.", nameof(localRestaurantTime));
        if (localRestaurantTime.Kind == DateTimeKind.Utc) return localRestaurantTime;
        if (TimeZone.IsInvalidTime(localRestaurantTime))
            throw new ArgumentException("Restaurant-local time falls in an invalid timezone interval.", nameof(localRestaurantTime));
        return TimeZoneInfo.ConvertTimeToUtc(localRestaurantTime, TimeZone);
    }

    public DateOnly StartOfWeek(DateOnly localDate)
    {
        var daysSinceMonday = ((int)localDate.DayOfWeek + 6) % 7;
        return localDate.AddDays(-daysSinceMonday);
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Local)
            throw new ArgumentException("UTC instants must not use the server's Local DateTime kind.", nameof(value));
        return value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
