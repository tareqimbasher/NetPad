using MediatR;
using NetPad.Application;
using NetPad.DotNet;

namespace NetPad.CQs;

public class CheckAppDependenciesQuery : Query<AppDependencyCheckResult>
{
    public class Handler : IRequestHandler<CheckAppDependenciesQuery, AppDependencyCheckResult>
    {
        public Task<AppDependencyCheckResult> Handle(CheckAppDependenciesQuery request, CancellationToken cancellationToken)
        {
            var dotnetSdkExePath = DotNetInfo.LocateDotNetExecutable();
            var dotNetEfToolExePath = DotNetInfo.LocateDotNetEfToolExecutable();

            var result = new AppDependencyCheckResult(
                DotNetInfo.GetDotNetRuntimeVersion().ToString(),
                dotnetSdkExePath == null ? null : DotNetInfo.GetDotNetSdkVersion(dotnetSdkExePath)?.ToString(),
                dotNetEfToolExePath == null ? null : DotNetInfo.GetDotNetEfToolVersion(dotNetEfToolExePath)?.ToString()
            );

            return Task.FromResult(result);
        }
    }
}
