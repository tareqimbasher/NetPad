using System.Collections.Concurrent;
using NuGet.Configuration;

namespace NetPad.Packages.NuGet;

/// <summary>
/// Tracks package sources that recently failed so callers can skip them for a cooldown period instead
/// of paying a network timeout (and a log entry) per package, per call. A skipped source re-enters
/// rotation when its cooldown lapses or a call to it succeeds.
/// </summary>
internal sealed class PackageSourceHealth(Func<DateTimeOffset>? clock = null, TimeSpan? cooldown = null)
{
    private readonly Func<DateTimeOffset> _clock = clock ?? (() => DateTimeOffset.UtcNow);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _failedAt = new();

    public TimeSpan Cooldown { get; } = cooldown ?? TimeSpan.FromMinutes(2);

    public bool ShouldSkip(PackageSource source)
    {
        return _failedAt.TryGetValue(Key(source), out var failedAt)
               && _clock() - failedAt < Cooldown;
    }

    /// <summary>
    /// Records a failed call to a source. Returns true when this starts a new unavailability episode,
    /// so callers can log the first failure and stay quiet about the rest.
    /// </summary>
    public bool RecordFailure(PackageSource source)
    {
        var now = _clock();
        var key = Key(source);

        bool newEpisode = !_failedAt.TryGetValue(key, out var previous) || now - previous >= Cooldown;
        _failedAt[key] = now;
        return newEpisode;
    }

    public void RecordSuccess(PackageSource source)
    {
        _failedAt.TryRemove(Key(source), out _);
    }

    // Keyed on the source URL/path.
    private static string Key(PackageSource source) => source.Source;
}
