using Microsoft.AspNetCore.Identity;

namespace Roms.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    // Removed staff keep their immutable Identity ID for schedules, attendance,
    // orders, and audit history. Their former login name is retained here while
    // UserName is changed to a unique archive key so the name can be reused.
    public string? ArchivedUserName { get; set; }
    public bool MustChangePassword { get; set; }
    // One server-issued session identifier is permitted per staff account.
    // It is deliberately not a personal-data field and is cleared on logout.
    public string? ActiveSessionId { get; set; }
    public DateTime? SessionLastActivityUtc { get; set; }

    public static string BuildArchivedUserName(string id, string oldUserName)
    {
        var prefix = $"__archived__{id[..Math.Min(12, id.Length)]}__";
        return prefix + oldUserName[..Math.Min(oldUserName.Length, 256 - prefix.Length)];
    }
}
