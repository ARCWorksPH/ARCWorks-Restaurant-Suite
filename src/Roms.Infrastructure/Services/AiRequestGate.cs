using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Roms.Infrastructure.Services;

public sealed class AiSecurityOptions
{
    public int MaxConcurrentRequests { get; set; } = 2;
    public int RequestsPerMinute { get; set; } = 6;
}

public enum AiRequestAdmissionStatus
{
    Accepted,
    UserRateLimited,
    CapacityReached
}

public sealed record AiRequestAdmission(
    AiRequestAdmissionStatus Status,
    IDisposable? Lease = null);

/// <summary>
/// In-process protection for the local model. It prevents one staff account
/// from monopolizing inference and bounds total concurrent Ollama work.
/// </summary>
public sealed class AiRequestGate : IDisposable
{
    private static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(1);
    private readonly ConcurrentDictionary<string, UserWindow> windows =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim capacity;
    private readonly int requestsPerMinute;

    public AiRequestGate(IOptions<AiSecurityOptions> options)
    {
        var value = options.Value;
        capacity = new SemaphoreSlim(Math.Clamp(value.MaxConcurrentRequests, 1, 16));
        requestsPerMinute = Math.Clamp(value.RequestsPerMinute, 1, 120);
    }

    public AiRequestAdmission TryAcquire(string actorUsername, DateTime utcNow)
    {
        var userWindow = windows.GetOrAdd(actorUsername, _ => new UserWindow());
        lock (userWindow.Sync)
        {
            while (userWindow.Requests.TryPeek(out var oldest) &&
                   utcNow - oldest >= WindowDuration)
            {
                userWindow.Requests.Dequeue();
            }

            if (userWindow.Requests.Count >= requestsPerMinute)
                return new(AiRequestAdmissionStatus.UserRateLimited);

            if (!capacity.Wait(0))
                return new(AiRequestAdmissionStatus.CapacityReached);

            userWindow.Requests.Enqueue(utcNow);
            return new(AiRequestAdmissionStatus.Accepted, new CapacityLease(capacity));
        }
    }

    public void Dispose() => capacity.Dispose();

    private sealed class UserWindow
    {
        public object Sync { get; } = new();
        public Queue<DateTime> Requests { get; } = new();
    }

    private sealed class CapacityLease(SemaphoreSlim semaphore) : IDisposable
    {
        private int disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
                semaphore.Release();
        }
    }
}
