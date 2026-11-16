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
            DotNetFrameworkVersion[]? frameworkVersionsMissingAspNetRuntime = null;

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
                frameworkVersionsMissingAspNetRuntime = dotNetInfo.GetSupportedFrameworkVersions()
                    .Where(v => !dotNetInfo.IsAspNetRuntimeInstalledFor(v))
                    .OrderBy(v => v)
                    .ToArray();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking for ASP.NET Core runtimes");
            }

            var result = new AppDependencyCheckResult(
                dotNetInfo.GetCurrentDotNetRuntimeVersion().ToString(),
                dotNetSdkVersions ?? [],
                frameworkVersionsMissingAspNetRuntime ?? []
            );

            return Task.FromResult(result);
        }
    }
}
