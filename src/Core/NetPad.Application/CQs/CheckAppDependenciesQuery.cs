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
            string? dotNetSdkVersion = null;
            string? dotNetEfToolVersion = null;

            try
            {
                var dotnetSdkExePath = DotNetInfo.LocateDotNetExecutable();

                dotNetSdkVersion = dotnetSdkExePath == null
                    ? null
                    : DotNetInfo.GetDotNetSdkVersion(dotnetSdkExePath)?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting .NET SDK version");
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
                DotNetInfo.GetDotNetRuntimeVersion().ToString(),
                dotNetSdkVersion,
                dotNetEfToolVersion
            );

            return Task.FromResult(result);
        }
    }
}
