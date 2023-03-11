using MediatR;
using Microsoft.Extensions.Logging;
using NetPad.Application;
using NetPad.DotNet;

namespace NetPad.CQs;

public class CheckAppDependenciesQuery : Query<AppDependencyCheckResult>
{
    public class Handler : IRequestHandler<CheckAppDependenciesQuery, AppDependencyCheckResult>
    {
        private readonly ILogger<Handler> _logger;

        public Handler(ILogger<Handler> logger)
        {
            _logger = logger;
        }

        public Task<AppDependencyCheckResult> Handle(CheckAppDependenciesQuery request,
            CancellationToken cancellationToken)
        {
            DotNetSdkVersion[]? dotNetSdkVersions = null;
            string? dotNetEfToolVersion = null;

            try
            {
                dotNetSdkVersions = DotNetInfo.GetDotNetSdkVersions();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting .NET SDK versions");
            }

            try
            {
                var dotNetEfToolExePath = DotNetInfo.LocateDotNetEfToolExecutable();

                dotNetEfToolVersion = dotNetEfToolExePath == null
                    ? null
                    : DotNetInfo.GetDotNetEfToolVersion(dotNetEfToolExePath)?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting .NET Entity Framework Tool version");
            }

            var result = new AppDependencyCheckResult(
                DotNetInfo.GetCurrentDotNetRuntimeVersion().ToString(),
                dotNetSdkVersions?.Select(v => v.Version.ToString()).ToArray() ?? Array.Empty<string>(),
                dotNetEfToolVersion
            );

            return Task.FromResult(result);
        }
    }
}
