using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NetPad.Application;
using NetPad.Plugins.OmniSharp.Features.Diagnostics;
using NetPad.Plugins.OmniSharp.Features.InlayHinting;
using NetPad.Plugins.OmniSharp.Services;
using OmniSharp.Models.v1.InlayHints;

namespace NetPad.Plugins.OmniSharp.Tests;

public sealed class OmniSharpMediatorPipelineTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public OmniSharpMediatorPipelineTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new OmniSharpServerCatalog(
            Mock.Of<IServiceScopeFactory>(),
            Mock.Of<IAppStatusMessagePublisher>(),
            NullLogger<OmniSharpServerCatalog>.Instance));

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Query_GetsNoResult_WhenScriptHasNoServer()
    {
        var pipeline = CreatePipeline<GetInlayHintsQuery, InlayHintResponse?>();
        var handlerCalled = false;

        var response = await pipeline.Handle(
            new GetInlayHintsQuery(Guid.NewGuid(), new InlayHintRequest()),
            CancellationToken.None,
            () =>
            {
                handlerCalled = true;
                return Task.FromResult<InlayHintResponse?>(new InlayHintResponse());
            });

        Assert.Null(response);
        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task Command_CompletesWithoutRunning_WhenScriptHasNoServer()
    {
        var pipeline = CreatePipeline<StartDiagnosticsCommand, Unit>();
        var handlerCalled = false;

        var response = await pipeline.Handle(
            new StartDiagnosticsCommand(Guid.NewGuid()),
            CancellationToken.None,
            () =>
            {
                handlerCalled = true;
                return Task.FromResult(Unit.Value);
            });

        Assert.Equal(Unit.Value, response);
        Assert.False(handlerCalled);
    }

    private OmniSharpMediatorPipeline<TRequest, TResponse> CreatePipeline<TRequest, TResponse>()
        where TRequest : class, IRequest<TResponse>
    {
        return new OmniSharpMediatorPipeline<TRequest, TResponse>(
            _serviceProvider,
            NullLogger<OmniSharpMediatorPipeline<TRequest, TResponse>>.Instance);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}
