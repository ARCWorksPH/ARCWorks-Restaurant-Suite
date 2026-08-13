using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Persistence;

namespace Roms.Web.Components.Account;

/// <summary>
/// Authoritative server owner of the one session and one live application
/// runtime permitted for each staff account.
/// </summary>
internal sealed class StaffSessionService(
    IDbContextFactory<RomsDbContext> factory,
    IConfiguration configuration,
    ILogger<StaffSessionService> logger)
{
    internal const string SessionClaimType = "arcworks:staff_session";
    internal const string SecurityReplayAction = "AuthenticatedSessionReplayDetected";

    private TimeSpan IdleTimeout => TimeSpan.FromMinutes(
        Math.Clamp(configuration.GetValue("Session:IdleTimeoutMinutes", 15), 5, 720));

    internal async Task<SessionStartResult> TryStartAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var cutoff = now - IdleTimeout;
        var sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var updated = await db.Users
            .Where(candidate => candidate.Id == user.Id &&
                (candidate.ActiveSessionId == null ||
                 candidate.SessionLastActivityUtc == null ||
                 candidate.SessionLastActivityUtc < cutoff))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(candidate => candidate.ActiveSessionId, sessionId)
                .SetProperty(candidate => candidate.ActiveApplicationInstanceId, (string?)null)
                .SetProperty(candidate => candidate.SessionLastActivityUtc, now), cancellationToken);

        if (updated == 0)
        {
            logger.LogWarning("Rejected a concurrent sign-in for staff user {UserId}.", user.Id);
            return SessionStartResult.AlreadyActive;
        }

        user.ActiveSessionId = sessionId;
        user.ActiveApplicationInstanceId = null;
        user.SessionLastActivityUtc = now;
        return new SessionStartResult(true, sessionId);
    }

    internal async Task<ApplicationInstanceRegistration> RegisterApplicationInstanceAsync(
        ClaimsPrincipal principal,
        string applicationInstanceId,
        CancellationToken cancellationToken = default)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionId = principal.FindFirstValue(SessionClaimType);
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionId) ||
            !IsValidInstanceId(applicationInstanceId)) return ApplicationInstanceRegistration.InvalidSession;

        var now = DateTime.UtcNow;
        var cutoff = now - IdleTimeout;
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var claimed = await db.Users
            .Where(user => user.Id == userId &&
                           user.ActiveSessionId == sessionId &&
                           user.ActiveApplicationInstanceId == null &&
                           user.SessionLastActivityUtc != null &&
                           user.SessionLastActivityUtc >= cutoff)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.ActiveApplicationInstanceId, applicationInstanceId)
                .SetProperty(user => user.SessionLastActivityUtc, now), cancellationToken);
        if (claimed != 0) return ApplicationInstanceRegistration.Accepted;

        var current = await db.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new
            {
                user.UserName,
                user.ActiveSessionId,
                user.ActiveApplicationInstanceId,
                user.SessionLastActivityUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (current is null || current.ActiveSessionId != sessionId ||
            current.SessionLastActivityUtc is null || current.SessionLastActivityUtc < cutoff ||
            string.IsNullOrWhiteSpace(current.ActiveApplicationInstanceId)) return ApplicationInstanceRegistration.InvalidSession;

        if (FixedTimeEquals(current.ActiveApplicationInstanceId, applicationInstanceId)) return ApplicationInstanceRegistration.Accepted;

        // A second runtime replayed the same authenticated cookie. Never guess
        // which copy is legitimate: revoke the entire session atomically.
        var revoked = await db.Users
            .Where(user => user.Id == userId &&
                           user.ActiveSessionId == sessionId &&
                           user.ActiveApplicationInstanceId == current.ActiveApplicationInstanceId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.ActiveSessionId, (string?)null)
                .SetProperty(user => user.ActiveApplicationInstanceId, (string?)null)
                .SetProperty(user => user.SessionLastActivityUtc, (DateTime?)null), cancellationToken);

        if (revoked != 0)
        {
            db.AuditEntries.Add(new Roms.Domain.AuditEntry
            {
                ActorId = "security-system",
                Action = SecurityReplayAction,
                EntityType = nameof(ApplicationUser),
                EntityId = userId,
                NewValuesJson = JsonSerializer.Serialize(new
                {
                    Username = current.UserName,
                    SessionFingerprint = Fingerprint(sessionId),
                    OriginalInstanceFingerprint = Fingerprint(current.ActiveApplicationInstanceId),
                    ReplayInstanceFingerprint = Fingerprint(applicationInstanceId)
                }),
                Reason = "A second application runtime presented the same authenticated staff session. The entire session was revoked.",
                OccurredUtc = now
            });
            await db.SaveChangesAsync(cancellationToken);
            logger.LogCritical(
                "Revoked staff session {SessionFingerprint} for user {UserId} after authenticated-session replay was detected.",
                Fingerprint(sessionId), userId);
        }

        return ApplicationInstanceRegistration.ReplayDetected;
    }

    internal async Task<bool> TouchAsync(
        ClaimsPrincipal principal,
        string applicationInstanceId,
        CancellationToken cancellationToken = default)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionId = principal.FindFirstValue(SessionClaimType);
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionId) ||
            !IsValidInstanceId(applicationInstanceId)) return false;

        var now = DateTime.UtcNow;
        var cutoff = now - IdleTimeout;
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var updated = await db.Users
            .Where(user => user.Id == userId &&
                           user.ActiveSessionId == sessionId &&
                           user.ActiveApplicationInstanceId == applicationInstanceId &&
                           user.SessionLastActivityUtc != null &&
                           user.SessionLastActivityUtc >= cutoff)
            .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.SessionLastActivityUtc, now), cancellationToken);

        if (updated != 0) return true;
        await ClearExpiredAsync(db, userId, sessionId, cutoff, cancellationToken);
        logger.LogInformation("Rejected activity for an expired, revoked, or non-owner staff runtime for user {UserId}.", userId);
        return false;
    }

    internal async Task EndAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionId = principal.FindFirstValue(SessionClaimType);
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionId)) return;

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await db.Users.Where(user => user.Id == userId && user.ActiveSessionId == sessionId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.ActiveSessionId, (string?)null)
                .SetProperty(user => user.ActiveApplicationInstanceId, (string?)null)
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
        return await db.Users.AnyAsync(user => user.Id == userId &&
            user.ActiveSessionId == sessionId && user.SessionLastActivityUtc != null &&
            user.SessionLastActivityUtc >= cutoff, cancellationToken);
    }

    private static Task<int> ClearExpiredAsync(RomsDbContext db, string userId, string sessionId,
        DateTime cutoff, CancellationToken cancellationToken) =>
        db.Users.Where(user => user.Id == userId && user.ActiveSessionId == sessionId &&
            (user.SessionLastActivityUtc == null || user.SessionLastActivityUtc < cutoff))
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(user => user.ActiveSessionId, (string?)null)
            .SetProperty(user => user.ActiveApplicationInstanceId, (string?)null)
            .SetProperty(user => user.SessionLastActivityUtc, (DateTime?)null), cancellationToken);

    private static bool IsValidInstanceId(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);

    private static bool FixedTimeEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(Convert.FromHexString(left), Convert.FromHexString(right));

    private static string Fingerprint(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16];
}

internal readonly record struct SessionStartResult(bool Started, string? SessionId)
{
    internal static SessionStartResult AlreadyActive => new(false, null);
}

internal enum ApplicationInstanceRegistration
{
    Accepted,
    InvalidSession,
    ReplayDetected
}
