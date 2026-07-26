using NetPad.Packages.NuGet;
using NuGet.Configuration;

namespace NetPad.Runtime.Tests.Packages.NuGet;

public class PackageSourceHealthTests
{
    private static readonly TimeSpan _cooldown = TimeSpan.FromMinutes(3);

    private readonly TestClock _clock = new();
    private readonly PackageSourceHealth _health;
    private readonly PackageSource _source = new("https://example.test/feed/index.json", "Example");

    public PackageSourceHealthTests()
    {
        _health = new PackageSourceHealth(() => _clock.Now, _cooldown);
    }

    [Fact]
    public void A_Source_Is_Not_Skipped_Before_Any_Failure()
    {
        Assert.False(_health.ShouldSkip(_source));
    }

    [Fact]
    public void The_First_Failure_Starts_An_Episode_And_The_Source_Is_Skipped()
    {
        Assert.True(_health.RecordFailure(_source));
        Assert.True(_health.ShouldSkip(_source));
    }

    [Fact]
    public void Repeat_Failures_Within_The_Cooldown_Are_Not_A_New_Episode()
    {
        _health.RecordFailure(_source);
        _clock.Advance(TimeSpan.FromSeconds(30));

        Assert.False(_health.RecordFailure(_source));
    }

    [Fact]
    public void A_Failure_Within_The_Cooldown_Extends_The_Skip_Window()
    {
        _health.RecordFailure(_source);
        _clock.Advance(TimeSpan.FromMinutes(2));
        _health.RecordFailure(_source);

        // Past the first failure's cooldown, but within the second's.
        _clock.Advance(TimeSpan.FromMinutes(2));

        Assert.True(_health.ShouldSkip(_source));
    }

    [Fact]
    public void The_Cooldown_Lapsing_Reinstates_The_Source_And_The_Next_Failure_Is_A_New_Episode()
    {
        _health.RecordFailure(_source);
        _clock.Advance(_cooldown);

        Assert.False(_health.ShouldSkip(_source));
        Assert.True(_health.RecordFailure(_source));
    }

    [Fact]
    public void A_Success_Reinstates_The_Source_And_The_Next_Failure_Is_A_New_Episode()
    {
        _health.RecordFailure(_source);
        _health.RecordSuccess(_source);

        Assert.False(_health.ShouldSkip(_source));
        Assert.True(_health.RecordFailure(_source));
    }

    [Fact]
    public void Sources_Are_Tracked_Independently()
    {
        var other = new PackageSource("https://other.test/feed/index.json", "Other");

        _health.RecordFailure(_source);

        Assert.False(_health.ShouldSkip(other));
    }

    [Fact]
    public void Differently_Named_Entries_For_The_Same_Location_Share_Health()
    {
        var sameLocation = new PackageSource(_source.Source, "Another name");

        _health.RecordFailure(_source);

        Assert.True(_health.ShouldSkip(sameLocation));
    }

    private sealed class TestClock
    {
        public DateTimeOffset Now { get; private set; } = new(2026, 7, 26, 12, 0, 0, TimeSpan.Zero);

        public void Advance(TimeSpan by) => Now += by;
    }
}
