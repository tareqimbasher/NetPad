using System.Runtime.InteropServices;
using MediatR;
using NetPad.Application;
using NetPad.Configuration;

namespace NetPad.Apps.CQs;

public class GetAppInfoQuery : Query<AppInfo>
{
    public class Handler(AppIdentifier appIdentifier, IMediator mediator) : IRequestHandler<GetAppInfoQuery, AppInfo>
    {
        public async Task<AppInfo> Handle(GetAppInfoQuery request, CancellationToken cancellationToken)
        {
            var dependencyCheckResult = await mediator.Send(new CheckAppDependenciesQuery(), cancellationToken);

            return new AppInfo(
                appIdentifier,
                dependencyCheckResult,
                AppDataProvider.AppDataDirectoryPath.Path,
                RuntimeInformation.OSDescription,
                RuntimeInformation.OSArchitecture);
        }
    }
}
