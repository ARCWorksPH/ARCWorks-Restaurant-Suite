using Roms.Application;

namespace Roms.Infrastructure.Services;

public sealed class SystemClock : IClock { public DateTime UtcNow => DateTime.UtcNow; }
