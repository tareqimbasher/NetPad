using MediatR;
using Microsoft.Extensions.Logging;
using NetPad.Application;
using NetPad.DotNet;

namespace NetPad.CQs;

public class CheckAppDependenciesQuery : Query<AppDependencyCheckResult>
{
    public class Handler : IRequestHandler<CheckAppDependenciesQuery, AppDependencyCheckResult>
    {
        private readonly IDotNetInfo _dotNetInfo;
        private readonly ILogger<Handler> _logger;

        public Handler(IDotNetInfo dotNetInfo, ILogger<Handler> logger)
        {
            _dotNetInfo = dotNetInfo;
            _logger = logger;
        }

        public Task<AppDependencyCheckResult> Handle(CheckAppDependenciesQuery request,
            CancellationToken cancellationToken)
        {
            DotNetSdkVersion[]? dotNetSdkVersions = null;
            string? dotNetEfToolVersion = null;

            try
            {
                dotNetSdkVersions = _dotNetInfo.GetDotNetSdkVersions();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting .NET SDK versions");
            }

            try
            {
                var dotNetEfToolExePath = _dotNetInfo.LocateDotNetEfToolExecutable();

                dotNetEfToolVersion = dotNetEfToolExePath == null
                    ? null
                    : _dotNetInfo.GetDotNetEfToolVersion(dotNetEfToolExePath)?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting .NET Entity Framework Tool version");
            }

            var result = new AppDependencyCheckResult(
                _dotNetInfo.GetCurrentDotNetRuntimeVersion().ToString(),
                dotNetSdkVersions?.Select(v => v.Version.ToString()).ToArray() ?? Array.Empty<string>(),
                dotNetEfToolVersion
            );

            return Task.FromResult(result);
        }
    }
}
