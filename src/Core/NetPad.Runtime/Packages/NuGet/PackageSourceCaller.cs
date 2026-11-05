using Microsoft.Extensions.Logging;
using NuGet.Configuration;

namespace NetPad.Packages.NuGet;

/// <summary>
/// Makes calls to package sources and keeps their <see cref="PackageSourceHealth"/> up to date.
/// </summary>
internal sealed class PackageSourceCaller(PackageSourceHealth health, ILogger logger)
{
    /// <summary>
    /// A failure after <paramref name="cancellationToken"/> is cancelled is the caller's cancellation,
    /// whatever exception NuGet wrapped it in: it is thrown as an <see cref="OperationCanceledException"/>
    /// and does not count against the source.
    /// </summary>
    public async Task<(bool Succeeded, T? Value)> CallAsync<T>(
        PackageSource source,
        string activity,
        Func<Task<T>> call,
        CancellationToken cancellationToken)
    {
        try
        {
            var value = await call().ConfigureAwait(false);
            health.RecordSuccess(source);
            return (true, value);
        }
        catch (Exception e) when (cancellationToken.IsCancellationRequested)
        {
            if (e is OperationCanceledException)
            {
                throw;
            }

            throw new OperationCanceledException($"{activity} was cancelled", e, cancellationToken);
        }
        catch (Exception e)
        {
            LogFailure(e, source, activity);
            return (false, default);
        }
    }

    /// <summary>
    /// A source failure is logged once per unavailability episode. For the cooldown that follows, the
    /// source is skipped and any residual failure logs at debug only.
    /// </summary>
    private void LogFailure(Exception e, PackageSource source, string activity)
    {
        if (health.RecordFailure(source))
        {
            logger.LogWarning(
                e,
                "Package source {Source} is unavailable ({Activity} failed). Skipping it for {Cooldown}",
                source.Name,
                activity,
                health.Cooldown);
        }
        else
        {
            logger.LogDebug(e, "Package source {Source} is still unavailable ({Activity} failed)", source.Name, activity);
        }
    }
}
