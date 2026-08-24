using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Persistence;

namespace Roms.Infrastructure.Services;

public sealed class StaffCommunicationService(
    IDbContextFactory<RomsDbContext> factory,
    IRestaurantClock restaurantClock) : IStaffCommunicationService
{
    private static readonly HashSet<string> AllowedAudienceRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        RomsRoles.Waiter, RomsRoles.Kitchen, RomsRoles.Manager, RomsRoles.Admin
    };

    public async Task<StaffHubView> GetStaffHubAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var waiter = await RequireActiveUserInRoleAsync(db, principal, RomsRoles.Waiter, cancellationToken);
        var nowUtc = restaurantClock.UtcNow;
        var localDate = restaurantClock.LocalDate;
        var dayStartUtc = restaurantClock.ToUtc(localDate.ToDateTime(TimeOnly.MinValue));
        var dayEndUtc = restaurantClock.ToUtc(localDate.AddDays(1).ToDateTime(TimeOnly.MinValue));

        var managerNote = await db.StaffSchedules.AsNoTracking()
            .Where(x => x.UserId == waiter.Id && x.ScheduledStartUtc < dayEndUtc && x.ScheduledEndUtc > dayStartUtc)
            .OrderBy(x => x.ScheduledStartUtc)
            .Select(x => x.Notes)
            .FirstOrDefaultAsync(cancellationToken);

        var announcements = await db.StaffAnnouncements.AsNoTracking()
            .Where(x => x.IsActive && x.PublishUtc <= nowUtc &&
                        (x.ExpiresUtc == null || x.ExpiresUtc > nowUtc) &&
                        (x.AudienceRole == null || x.AudienceRole == RomsRoles.Waiter))
            .OrderByDescending(x => x.Priority)
            .ThenByDescending(x => x.PublishUtc)
            .ToListAsync(cancellationToken);

        var ids = announcements.Select(x => x.Id).ToList();
        var receipts = ids.Count == 0
            ? []
            : await db.StaffAnnouncementReceipts.AsNoTracking()
                .Where(x => x.UserId == waiter.Id && ids.Contains(x.AnnouncementId))
                .ToListAsync(cancellationToken);
        var currentReceipts = receipts.ToDictionary(x => (x.AnnouncementId, x.AnnouncementVersion));

        var visible = announcements
            .Where(x => !currentReceipts.TryGetValue((x.Id, x.Version), out var receipt) || receipt.DismissedUtc is null)
            .Select(x =>
            {
                currentReceipts.TryGetValue((x.Id, x.Version), out var receipt);
                return new StaffAnnouncementView(
                    x.Id,
                    x.Version,
                    x.Title,
                    x.Body,
                    x.Priority,
                    restaurantClock.ToLocal(x.PublishUtc),
                    x.ExpiresUtc is null ? null : restaurantClock.ToLocal(x.ExpiresUtc.Value),
                    x.Priority == StaffAnnouncementPriority.Urgent,
                    receipt?.AcknowledgedUtc is not null);
            })
            .ToList();

        return new StaffHubView(string.IsNullOrWhiteSpace(managerNote) ? null : managerNote, visible);
    }

    public async Task<Guid> CreateAnnouncementAsync(ClaimsPrincipal actor, string title, string body,
        StaffAnnouncementPriority priority, string? audienceRole, DateTime publishUtc, DateTime? expiresUtc,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var author = await RequireManagerOrAdminAsync(db, actor, cancellationToken);
        audienceRole = NormalizeAudience(audienceRole);
        var announcement = new StaffAnnouncement
        {
            CreatedBy = author.UserName,
            CreatedUtc = restaurantClock.UtcNow
        };
        announcement.Configure(title, body, priority, audienceRole, AsUtc(publishUtc), AsNullableUtc(expiresUtc),
            author.UserName, restaurantClock.UtcNow);
        db.StaffAnnouncements.Add(announcement);
        db.AuditEntries.Add(Audit(author.UserName, "CreateStaffAnnouncement", announcement.Id, null,
            Snapshot(announcement)));
        await db.SaveChangesAsync(cancellationToken);
        return announcement.Id;
    }

    public async Task UpdateAnnouncementAsync(ClaimsPrincipal actor, Guid announcementId, string title, string body,
        StaffAnnouncementPriority priority, string? audienceRole, DateTime publishUtc, DateTime? expiresUtc,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var author = await RequireManagerOrAdminAsync(db, actor, cancellationToken);
        var announcement = await db.StaffAnnouncements.SingleOrDefaultAsync(x => x.Id == announcementId, cancellationToken)
            ?? throw new DomainException("Announcement not found.");
        var oldValues = Snapshot(announcement);
        announcement.Configure(title, body, priority, NormalizeAudience(audienceRole), AsUtc(publishUtc),
            AsNullableUtc(expiresUtc), author.UserName, restaurantClock.UtcNow, isEdit: true);
        db.AuditEntries.Add(Audit(author.UserName, "UpdateStaffAnnouncement", announcement.Id, oldValues,
            Snapshot(announcement)));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetAnnouncementActiveAsync(ClaimsPrincipal actor, Guid announcementId, bool active,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var author = await RequireManagerOrAdminAsync(db, actor, cancellationToken);
        var announcement = await db.StaffAnnouncements.SingleOrDefaultAsync(x => x.Id == announcementId, cancellationToken)
            ?? throw new DomainException("Announcement not found.");
        var oldValues = Snapshot(announcement);
        announcement.SetActive(active, author.UserName, restaurantClock.UtcNow);
        db.AuditEntries.Add(Audit(author.UserName, active ? "ActivateStaffAnnouncement" : "DeactivateStaffAnnouncement",
            announcement.Id, oldValues, Snapshot(announcement)));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AcknowledgeAsync(ClaimsPrincipal principal, Guid announcementId, int version,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var waiter = await RequireActiveUserInRoleAsync(db, principal, RomsRoles.Waiter, cancellationToken);
        var announcement = await RequireVisibleCurrentAnnouncementAsync(db, waiter.Id, announcementId, version, cancellationToken);
        if (announcement.Priority != StaffAnnouncementPriority.Urgent)
            throw new DomainException("Only urgent announcements require acknowledgment.");
        var receipt = await GetOrCreateReceiptAsync(db, waiter.Id, announcement, cancellationToken);
        if (receipt.AcknowledgedUtc is null)
        {
            receipt.Acknowledge(restaurantClock.UtcNow);
            db.AuditEntries.Add(Audit(waiter.UserName, "AcknowledgeStaffAnnouncement", announcement.Id, null,
                JsonSerializer.Serialize(new { announcement.Version, receipt.AcknowledgedUtc })));
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DismissAsync(ClaimsPrincipal principal, Guid announcementId, int version,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var waiter = await RequireActiveUserInRoleAsync(db, principal, RomsRoles.Waiter, cancellationToken);
        var announcement = await RequireVisibleCurrentAnnouncementAsync(db, waiter.Id, announcementId, version, cancellationToken);
        var receipt = await GetOrCreateReceiptAsync(db, waiter.Id, announcement, cancellationToken);
        if (announcement.Priority == StaffAnnouncementPriority.Urgent && receipt.AcknowledgedUtc is null)
            throw new DomainException("Urgent announcements must be acknowledged before dismissal.");
        if (receipt.DismissedUtc is null)
        {
            receipt.Dismiss(restaurantClock.UtcNow);
            db.AuditEntries.Add(Audit(waiter.UserName, "DismissStaffAnnouncement", announcement.Id, null,
                JsonSerializer.Serialize(new { announcement.Version, receipt.DismissedUtc })));
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<StaffAnnouncement> RequireVisibleCurrentAnnouncementAsync(RomsDbContext db, string userId,
        Guid announcementId, int version, CancellationToken cancellationToken)
    {
        var nowUtc = restaurantClock.UtcNow;
        var announcement = await db.StaffAnnouncements.SingleOrDefaultAsync(x =>
            x.Id == announcementId && x.Version == version && x.IsActive && x.PublishUtc <= nowUtc &&
            (x.ExpiresUtc == null || x.ExpiresUtc > nowUtc) &&
            (x.AudienceRole == null || x.AudienceRole == RomsRoles.Waiter), cancellationToken)
            ?? throw new DomainException("The announcement is not available to this staff member.");
        var dismissed = await db.StaffAnnouncementReceipts.AnyAsync(x => x.AnnouncementId == announcement.Id &&
            x.UserId == userId && x.AnnouncementVersion == version && x.DismissedUtc != null, cancellationToken);
        if (dismissed) throw new DomainException("The announcement has already been dismissed.");
        return announcement;
    }

    private static async Task<StaffAnnouncementReceipt> GetOrCreateReceiptAsync(RomsDbContext db, string userId,
        StaffAnnouncement announcement, CancellationToken cancellationToken)
    {
        var receipt = await db.StaffAnnouncementReceipts.SingleOrDefaultAsync(x =>
            x.AnnouncementId == announcement.Id && x.UserId == userId &&
            x.AnnouncementVersion == announcement.Version, cancellationToken);
        if (receipt is not null) return receipt;
        receipt = new StaffAnnouncementReceipt
        {
            AnnouncementId = announcement.Id,
            UserId = userId,
            AnnouncementVersion = announcement.Version
        };
        db.StaffAnnouncementReceipts.Add(receipt);
        return receipt;
    }

    private static async Task<StaffActor> RequireActiveUserInRoleAsync(RomsDbContext db, ClaimsPrincipal principal,
        string requiredRole, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(principal);
        if (principal.Identity?.IsAuthenticated != true) throw new DomainException("An authenticated staff identity is required.");
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) throw new DomainException("An authenticated staff identity is required.");
        return await (from user in db.Users
                      join userRole in db.UserRoles on user.Id equals userRole.UserId
                      join role in db.Roles on userRole.RoleId equals role.Id
                      where user.Id == userId && user.IsActive && user.UserName != null && role.Name == requiredRole
                      select new StaffActor(user.Id, user.UserName!)).SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException($"An active {requiredRole} account is required.");
    }

    private static async Task<StaffActor> RequireManagerOrAdminAsync(RomsDbContext db, ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(principal);
        if (principal.Identity?.IsAuthenticated != true) throw new DomainException("An authenticated staff identity is required.");
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) throw new DomainException("An authenticated staff identity is required.");
        return await (from user in db.Users
                      join userRole in db.UserRoles on user.Id equals userRole.UserId
                      join role in db.Roles on userRole.RoleId equals role.Id
                      where user.Id == userId && user.IsActive && user.UserName != null &&
                            (role.Name == RomsRoles.Manager || role.Name == RomsRoles.Admin)
                      select new StaffActor(user.Id, user.UserName!)).Distinct().SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("Only a manager or administrator can manage announcements.");
    }

    private static string? NormalizeAudience(string? audienceRole)
    {
        if (string.IsNullOrWhiteSpace(audienceRole)) return null;
        var canonical = AllowedAudienceRoles.FirstOrDefault(x => x.Equals(audienceRole.Trim(), StringComparison.OrdinalIgnoreCase));
        return canonical ?? throw new DomainException("Announcement audience role is not supported.");
    }

    private static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Utc
        ? value
        : throw new DomainException("Announcement times must be UTC.");

    private static DateTime? AsNullableUtc(DateTime? value) => value is null ? null : AsUtc(value.Value);

    private AuditEntry Audit(string actorId, string action, Guid id, string? oldValues, string? newValues) => new()
    {
        ActorId = actorId,
        Action = action,
        EntityType = nameof(StaffAnnouncement),
        EntityId = id.ToString(),
        OldValuesJson = oldValues,
        NewValuesJson = newValues,
        OccurredUtc = restaurantClock.UtcNow
    };

    private static string Snapshot(StaffAnnouncement announcement) => JsonSerializer.Serialize(new
    {
        announcement.Version,
        announcement.Title,
        announcement.Body,
        announcement.Priority,
        announcement.AudienceRole,
        announcement.PublishUtc,
        announcement.ExpiresUtc,
        announcement.IsActive
    });

    private sealed record StaffActor(string Id, string UserName);
}
