using NetPad.Application;

namespace NetPad.CQs;

public class AlertUserAboutMissingAppDependencies : Command
{
    public AlertUserAboutMissingAppDependencies(AppDependencyCheckResult dependencyCheckResult)
    {
        DependencyCheckResult = dependencyCheckResult;
    }

    public AppDependencyCheckResult DependencyCheckResult { get; }
}
