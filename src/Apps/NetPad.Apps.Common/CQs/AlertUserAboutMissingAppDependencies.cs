using NetPad.Application;

namespace NetPad.Apps.CQs;

public class AlertUserAboutMissingAppDependencies(AppDependencyCheckResult dependencyCheckResult) : Command
{
    public AppDependencyCheckResult DependencyCheckResult { get; } = dependencyCheckResult;
}
