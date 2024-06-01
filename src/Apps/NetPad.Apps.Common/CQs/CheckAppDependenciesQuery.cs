using MediatR;
using Microsoft.Extensions.Logging;
using NetPad.Application;
using NetPad.DotNet;

namespace NetPad.Apps.CQs;

public class CheckAppDependenciesQuery : Query<AppDependencyCheckResult>
{
    public class Handler(IDotNetInfo dotNetInfo, ILogger<Handler> logger)
        : IRequestHandler<CheckAppDependenciesQuery, AppDependencyCheckResult>
    {
        public Task<AppDependencyCheckResult> Handle(CheckAppDependenciesQuery request,
            CancellationToken cancellationToken)
        {
            DotNetSdkVersion[]? dotNetSdkVersions = null;
            SemanticVersion? dotNetEfToolVersion = null;

            try
            {
                dotNetSdkVersions = dotNetInfo.GetDotNetSdkVersions();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting .NET SDK versions");
            }

            try
            {
                var dotNetEfToolExePath = dotNetInfo.LocateDotNetEfToolExecutable();

                dotNetEfToolVersion = dotNetEfToolExePath == null
                    ? null
                    : dotNetInfo.GetDotNetEfToolVersion(dotNetEfToolExePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting .NET Entity Framework Tool version");
            }

            var result = new AppDependencyCheckResult(
                dotNetInfo.GetCurrentDotNetRuntimeVersion().ToString(),
                dotNetSdkVersions?.Select(v => v.Version).ToArray() ?? [],
                dotNetEfToolVersion
            );

            return Task.FromResult(result);
        }
    }
}
