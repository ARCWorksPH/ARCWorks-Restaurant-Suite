using Microsoft.AspNetCore.Identity;
using Roms.Domain;

namespace Roms.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    // Stored as an app-relative, local static asset path. External URLs are not
    // accepted so a staff profile cannot turn the dashboard into a tracker.
    public string? ProfilePortraitPath { get; set; }
    public StaffProfileLifecycle ProfileLifecycle { get; set; } = StaffProfileLifecycle.Approved;
    public DateTime? ProfileUpdatedUtc { get; set; }
    // Fixture data is explicitly labelled so it can be identified and removed
    // before restaurant-approved staff data is entered.
    public bool IsDemoProfile { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    // Removed staff keep their immutable Identity ID for schedules, attendance,
    // orders, and audit history. Their former login name is retained here while
    // UserName is changed to a unique archive key so the name can be reused.
    public string? ArchivedUserName { get; set; }
    public bool MustChangePassword { get; set; }
    // One server-issued session identifier is permitted per staff account.
    // It is deliberately not a personal-data field and is cleared on logout.
    public string? ActiveSessionId { get; set; }
    // Memory-only browser runtime owner. Copying the authentication cookie or
    // browser profile starts a different runtime and must not inherit access.
    public string? ActiveApplicationInstanceId { get; set; }
    public DateTime? SessionLastActivityUtc { get; set; }

    public static string BuildArchivedUserName(string id, string oldUserName)
    {
        var prefix = $"__archived__{id[..Math.Min(12, id.Length)]}__";
        return prefix + oldUserName[..Math.Min(oldUserName.Length, 256 - prefix.Length)];
    }
}
