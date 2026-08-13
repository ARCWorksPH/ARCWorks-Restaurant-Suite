using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Persistence;

namespace Roms.Web.Components.Account;

/// <summary>
/// Server-backed owner of a staff account's one permitted active session.
/// Browser-side timers improve the experience, but this service is the actual
/// enforcement point for a second-device login and an expired inactive session.
/// </summary>
internal sealed class StaffSessionService(
    IDbContextFactory<RomsDbContext> factory,
    IConfiguration configuration,
    ILogger<StaffSessionService> logger)
{
    internal const string SessionClaimType = "arcworks:staff_session";

    private TimeSpan IdleTimeout => TimeSpan.FromMinutes(
        Math.Clamp(configuration.GetValue("Session:IdleTimeoutMinutes", 15), 5, 720));

    internal async Task<SessionStartResult> TryStartAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var cutoff = now - IdleTimeout;
        var sessionId = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var updated = await db.Users
            .Where(candidate => candidate.Id == user.Id &&
                (candidate.ActiveSessionId == null ||
                 candidate.SessionLastActivityUtc == null ||
                 candidate.SessionLastActivityUtc < cutoff))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(candidate => candidate.ActiveSessionId, sessionId)
                .SetProperty(candidate => candidate.SessionLastActivityUtc, now), cancellationToken);

        if (updated == 0)
        {
            logger.LogWarning("Rejected a concurrent sign-in for staff user {UserId}.", user.Id);
            return SessionStartResult.AlreadyActive;
        }

        // Keep the principal factory and the just-issued authentication cookie
        // in sync without writing the identity row a second time.
        user.ActiveSessionId = sessionId;
        user.SessionLastActivityUtc = now;
        return new SessionStartResult(true, sessionId);
    }

    internal async Task<bool> TouchAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionId = principal.FindFirstValue(SessionClaimType);
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionId)) return false;

        var now = DateTime.UtcNow;
        var cutoff = now - IdleTimeout;
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var updated = await db.Users
            .Where(user => user.Id == userId &&
                           user.ActiveSessionId == sessionId &&
                           user.SessionLastActivityUtc != null &&
                           user.SessionLastActivityUtc >= cutoff)
            .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.SessionLastActivityUtc, now), cancellationToken);

        if (updated != 0) return true;

        await ClearExpiredAsync(db, userId, sessionId, cutoff, cancellationToken);
        logger.LogInformation("Rejected activity for an expired or revoked staff session for user {UserId}.", userId);
        return false;
    }

    internal async Task EndAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionId = principal.FindFirstValue(SessionClaimType);
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionId)) return;

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await db.Users
            .Where(user => user.Id == userId && user.ActiveSessionId == sessionId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.ActiveSessionId, (string?)null)
                .SetProperty(user => user.SessionLastActivityUtc, (DateTime?)null), cancellationToken);
    }

    internal async Task<bool> IsCurrentAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionId = principal.FindFirstValue(SessionClaimType);
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionId)) return false;

        var cutoff = DateTime.UtcNow - IdleTimeout;
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await ClearExpiredAsync(db, userId, sessionId, cutoff, cancellationToken);
        return await db.Users.AnyAsync(user =>
            user.Id == userId &&
            user.ActiveSessionId == sessionId &&
            user.SessionLastActivityUtc != null &&
            user.SessionLastActivityUtc >= cutoff,
            cancellationToken);
    }

    private static Task<int> ClearExpiredAsync(
        RomsDbContext db,
        string userId,
        string sessionId,
        DateTime cutoff,
        CancellationToken cancellationToken) =>
        db.Users
            .Where(user => user.Id == userId &&
                           user.ActiveSessionId == sessionId &&
                           (user.SessionLastActivityUtc == null || user.SessionLastActivityUtc < cutoff))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.ActiveSessionId, (string?)null)
                .SetProperty(user => user.SessionLastActivityUtc, (DateTime?)null), cancellationToken);
}

internal readonly record struct SessionStartResult(bool Started, string? SessionId)
{
    internal static SessionStartResult AlreadyActive => new(false, null);
}
