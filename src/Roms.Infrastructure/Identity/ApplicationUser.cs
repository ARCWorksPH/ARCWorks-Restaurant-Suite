using Microsoft.AspNetCore.Identity;

namespace Roms.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    // One server-issued session identifier is permitted per staff account.
    // It is deliberately not a personal-data field and is cleared on logout.
    public string? ActiveSessionId { get; set; }
    public DateTime? SessionLastActivityUtc { get; set; }
}
