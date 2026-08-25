using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Services;

namespace Roms.IntegrationTests;

[Collection(MariaDbCollection.Name)]
public sealed class MariaDbStaffCommunicationTests(MariaDbFixture fixture)
{
    [Fact]
    public async Task Staff_hub_loads_announcements_and_receipts_on_real_MariaDB()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using (var db = database.CreateContext())
        {
            var waiterRole = new IdentityRole(RomsRoles.Waiter)
            {
                Id = "waiter-role",
                NormalizedName = RomsRoles.Waiter.ToUpperInvariant()
            };
            var managerRole = new IdentityRole(RomsRoles.Manager)
            {
                Id = "manager-role",
                NormalizedName = RomsRoles.Manager.ToUpperInvariant()
            };
            db.Roles.AddRange(waiterRole, managerRole);
            db.Users.AddRange(
                new ApplicationUser
                {
                    Id = "waiter-1", UserName = "waiter-one", NormalizedUserName = "WAITER-ONE",
                    DisplayName = "Waiter One", IsActive = true
                },
                new ApplicationUser
                {
                    Id = "manager-1", UserName = "manager-one", NormalizedUserName = "MANAGER-ONE",
                    DisplayName = "Manager One", IsActive = true
                });
            db.UserRoles.AddRange(
                new IdentityUserRole<string> { UserId = "waiter-1", RoleId = waiterRole.Id },
                new IdentityUserRole<string> { UserId = "manager-1", RoleId = managerRole.Id });
            await db.SaveChangesAsync();
        }

        var clock = new FixedRestaurantClock(new DateTime(2026, 8, 25, 10, 0, 0, DateTimeKind.Utc));
        var service = new StaffCommunicationService(database.CreateFactory(), clock);
        var announcementId = await service.CreateAnnouncementAsync(
            Principal("manager-1", "manager-one"), "Service briefing", "Check today's assignments.",
            StaffAnnouncementPriority.Normal, RomsRoles.Waiter, clock.UtcNow.AddMinutes(-1), null);

        var first = await service.GetStaffHubAsync(Principal("waiter-1", "waiter-one"));
        var announcement = Assert.Single(first.Announcements);
        Assert.Equal(announcementId, announcement.Id);

        await service.DismissAsync(Principal("waiter-1", "waiter-one"), announcement.Id, announcement.Version);
        Assert.Empty((await service.GetStaffHubAsync(Principal("waiter-1", "waiter-one"))).Announcements);
    }

    private static ClaimsPrincipal Principal(string id, string username) => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Name, username)
    }, "Gate2HMariaDbTest"));

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
