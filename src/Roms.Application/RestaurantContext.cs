namespace Roms.Application;

/// <summary>
/// Server-authoritative restaurant calendar and display clock. Persisted
/// instants remain UTC; this boundary is only for restaurant-local display and
/// calendar calculations.
/// </summary>
public interface IRestaurantClock
{
    DateTime UtcNow { get; }
    DateTime LocalNow { get; }
    DateOnly LocalDate { get; }
    TimeZoneInfo TimeZone { get; }
    DateTime ToLocal(DateTime utcInstant);
    DateTime ToUtc(DateTime localRestaurantTime);
    DateOnly StartOfWeek(DateOnly localDate);
}
