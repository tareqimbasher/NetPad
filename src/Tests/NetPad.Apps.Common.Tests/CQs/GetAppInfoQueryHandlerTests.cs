using System.Runtime.InteropServices;
using MediatR;
using Moq;
using NetPad.Application;
using NetPad.Apps.CQs;
using NetPad.Configuration;

namespace NetPad.Apps.Common.Tests.CQs;

public class GetAppInfoQueryHandlerTests
{
    [Fact]
    public async Task Composes_Identifier_Dependency_Check_App_Data_Path_And_Os()
    {
        var appIdentifier = new AppIdentifier();
        var dependencyCheckResult = new AppDependencyCheckResult("9.0.0", [], null);

        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<CheckAppDependenciesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dependencyCheckResult);

        var handler = new GetAppInfoQuery.Handler(appIdentifier, mediator.Object);

        var info = await handler.Handle(new GetAppInfoQuery(), CancellationToken.None);

        Assert.Same(appIdentifier, info.Identifier);
        Assert.Same(dependencyCheckResult, info.DependencyCheckResult);
        Assert.Equal(AppDataProvider.AppDataDirectoryPath.Path, info.AppDataDirectoryPath);
        Assert.Equal(RuntimeInformation.OSDescription, info.OsDescription);
        Assert.Equal(RuntimeInformation.OSArchitecture, info.OsArchitecture);
        mediator.Verify(
            m => m.Send(It.IsAny<CheckAppDependenciesQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
