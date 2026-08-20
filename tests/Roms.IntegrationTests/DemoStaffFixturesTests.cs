using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Persistence;
using Roms.Web;

namespace Roms.IntegrationTests;

public sealed class DemoStaffFixturesTests
{
    [Fact]
    public async Task Development_fixtures_create_ten_non_sign_in_profiles_with_schedules_and_history()
    {
        var options = new DbContextOptionsBuilder<RomsDbContext>()
            .UseInMemoryDatabase($"roms-demo-staff-{Guid.NewGuid():N}")
            .Options;
        await using var db = new RomsDbContext(options);
        await db.Database.EnsureCreatedAsync();
        db.Roles.AddRange(new[] { RomsRoles.Admin, RomsRoles.Waiter, RomsRoles.Kitchen, RomsRoles.Manager }.Select(role => new IdentityRole(role)
        {
            Id = $"{role.ToLowerInvariant()}-role-id",
            NormalizedName = role.ToUpperInvariant()
        }));
        await db.SaveChangesAsync();

        var clock = new FixedRestaurantClock(new DateTime(2026, 8, 19, 4, 0, 0, DateTimeKind.Utc));
        using var store = new UserStore<ApplicationUser, IdentityRole, RomsDbContext>(db);
        using var users = new UserManager<ApplicationUser>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

        await DemoStaffFixtures.EnsureAsync(db, users, clock);
        await DemoStaffFixtures.EnsureAsync(db, users, clock);

        var profiles = await db.Users.Where(user => user.IsDemoProfile).ToListAsync();
        Assert.Equal(10, profiles.Count);
        Assert.All(profiles, profile =>
        {
            Assert.True(profile.IsActive);
            Assert.Equal(StaffProfileLifecycle.Approved, profile.ProfileLifecycle);
            Assert.StartsWith("/images/staff/demo/team-", profile.ProfilePortraitPath);
            Assert.Null(profile.PasswordHash);
        });

        Assert.Equal(70, await db.StaffSchedules.CountAsync());
        Assert.Equal(30, await db.AttendanceRecords.CountAsync());
        Assert.Equal(80, await db.AuditEntries.CountAsync());
        Assert.Equal(10, await db.UserRoles.CountAsync());
    }

    private sealed class FixedRestaurantClock(DateTime utcNow) : IRestaurantClock
    {
        private static readonly TimeZoneInfo Manila = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
        public DateTime UtcNow { get; } = utcNow;
        public DateTime LocalNow => ToLocal(UtcNow);
        public DateOnly LocalDate => DateOnly.FromDateTime(LocalNow);
        public TimeZoneInfo TimeZone => Manila;
        public DateTime ToLocal(DateTime utcInstant) => TimeZoneInfo.ConvertTimeFromUtc(utcInstant, Manila);
        public DateTime ToUtc(DateTime localRestaurantTime) => TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localRestaurantTime, DateTimeKind.Unspecified), Manila);
        public DateOnly StartOfWeek(DateOnly localDate) => localDate.AddDays(-(((int)localDate.DayOfWeek + 6) % 7));
    }
}
