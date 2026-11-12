using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NetPad.Apps.Data;
using NetPad.Data;
using NetPad.Data.Events;
using NetPad.Data.Metadata;
using NetPad.Data.Metadata.ChangeDetection;
using NetPad.DotNet;
using NetPad.Events;

namespace NetPad.Apps.Common.Tests.Data;

public class FileSystemDataConnectionResourcesCacheTests
{
    private const DotNetFrameworkVersion Framework = DotNetFrameworkVersion.DotNet10;

    private static readonly TimeSpan _testTimeout = TimeSpan.FromSeconds(5);

    private readonly DataConnection _connection = CreateDataConnection();
    private readonly EventBus _eventBus = new();
    private readonly ConcurrentQueue<DataConnectionResourcesUpdateProgressEvent> _progressEvents = new();
    private readonly Mock<IDataConnectionSchemaChangeDetectionStrategyFactory> _strategyFactory = new();
    private readonly Mock<IDataConnectionResourcesGeneratorFactory> _generatorFactory = new();
    private DataConnectionResources? _diskCachedResources;
    private Func<Action<DataConnectionSetupStage>?, Task> _generate = _ => Task.CompletedTask;

    public FileSystemDataConnectionResourcesCacheTests()
    {
        _eventBus.Subscribe<DataConnectionResourcesUpdateProgressEvent>(ev =>
        {
            _progressEvents.Enqueue(ev);
            return Task.CompletedTask;
        });

        _strategyFactory
            .Setup(f => f.GetStrategies(It.IsAny<DataConnection>()))
            .Returns([]);

        _generatorFactory
            .Setup(f => f.Create(It.IsAny<DataConnection>()))
            .Returns(() => new FakeGenerator(_generate));
    }

    [Fact]
    public async Task Generation_PublishesOneProgressEventPerStageInOrder()
    {
        _generate = ReportStages(DataConnectionSetupStage.Scaffolding, DataConnectionSetupStage.Compiling);
        var cache = CreateCache();

        await cache.GetResourcesAsync(_connection, Framework);

        Assert.Equal(
            [DataConnectionSetupStage.Scaffolding, DataConnectionSetupStage.Compiling],
            _progressEvents.Select(e => e.Stage));
        Assert.All(_progressEvents, e =>
        {
            Assert.Same(_connection, e.DataConnection);
            Assert.Equal(Framework, e.TargetFrameworkVersion);
            Assert.Equal(e.Stage.GetDescription(), e.StageDescription);
        });
    }

    [Fact]
    public async Task Generation_ReportsEachStageToCaller()
    {
        _generate = ReportStages(DataConnectionSetupStage.Scaffolding, DataConnectionSetupStage.Compiling);
        var cache = CreateCache();
        var stages = new List<DataConnectionSetupStage>();

        await cache.GetResourcesAsync(_connection, Framework, stages.Add);

        Assert.Equal([DataConnectionSetupStage.Scaffolding, DataConnectionSetupStage.Compiling], stages);
    }

    [Fact]
    public async Task CallerJoiningGenerationInProgress_GetsCurrentStageAtOnceThenLaterStages()
    {
        var atGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _generate = async reportStage =>
        {
            reportStage?.Invoke(DataConnectionSetupStage.RestoringPackages);
            reportStage?.Invoke(DataConnectionSetupStage.Scaffolding);
            atGate.SetResult();
            await gate.Task;
            reportStage?.Invoke(DataConnectionSetupStage.Compiling);
        };
        var cache = CreateCache();
        var joiningCallerStages = new List<DataConnectionSetupStage>();

        var firstCall = cache.GetResourcesAsync(_connection, Framework);
        await atGate.Task.WaitAsync(_testTimeout);
        var joiningCall = cache.GetResourcesAsync(_connection, Framework, joiningCallerStages.Add);

        Assert.Equal([DataConnectionSetupStage.Scaffolding], joiningCallerStages);

        gate.SetResult();
        await Task.WhenAll(firstCall, joiningCall).WaitAsync(_testTimeout);

        Assert.Equal([DataConnectionSetupStage.Scaffolding, DataConnectionSetupStage.Compiling], joiningCallerStages);
    }

    [Fact]
    public async Task CurrentStage_IsClearedAfterGenerationSucceeds()
    {
        _generate = ReportStages(DataConnectionSetupStage.Scaffolding);
        var cache = CreateCache();
        await cache.GetResourcesAsync(_connection, Framework);
        await cache.RemoveCachedResourcesAsync(_connection.Id);

        var stages = new List<DataConnectionSetupStage>();
        _generate = _ => Task.CompletedTask;
        await cache.GetResourcesAsync(_connection, Framework, stages.Add);

        Assert.Empty(stages);
    }

    [Fact]
    public async Task CurrentStage_IsClearedAfterGenerationFails()
    {
        _generate = reportStage =>
        {
            reportStage?.Invoke(DataConnectionSetupStage.Scaffolding);
            throw new InvalidOperationException("Scaffold failed");
        };
        var cache = CreateCache();
        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetResourcesAsync(_connection, Framework));

        var stages = new List<DataConnectionSetupStage>();
        _generate = _ => Task.CompletedTask;
        await cache.GetResourcesAsync(_connection, Framework, stages.Add);

        Assert.Empty(stages);
    }

    [Fact]
    public async Task ThrowingCallback_DoesNotFailGeneration()
    {
        _generate = ReportStages(DataConnectionSetupStage.Scaffolding, DataConnectionSetupStage.Compiling);
        var cache = CreateCache();

        var resources = await cache.GetResourcesAsync(
            _connection,
            Framework,
            _ => throw new InvalidOperationException("Callback failed"));

        Assert.Same(_connection, resources.DataConnection);
        Assert.Equal(2, _progressEvents.Count);
    }

    [Fact]
    public async Task CallerWithoutCallback_GetsResources()
    {
        _generate = ReportStages(DataConnectionSetupStage.Scaffolding);
        var cache = CreateCache();

        var resources = await cache.GetResourcesAsync(_connection, Framework);

        Assert.Same(_connection, resources.DataConnection);
        Assert.Single(_progressEvents);
    }

    [Fact]
    public async Task CheckingSchema_IsReportedOnlyToCallerThatChecks()
    {
        var atGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _generate = async reportStage =>
        {
            reportStage?.Invoke(DataConnectionSetupStage.Scaffolding);
            atGate.SetResult();
            await gate.Task;
        };
        var cache = CreateCache();
        var generatingCallerStages = new List<DataConnectionSetupStage>();
        var checkingCallerStages = new List<DataConnectionSetupStage>();

        var generatingCall = cache.GetResourcesAsync(_connection, Framework, generatingCallerStages.Add);
        await atGate.Task.WaitAsync(_testTimeout);

        _diskCachedResources = new DataConnectionResources(_connection, DateTime.UtcNow);
        SetupSchemaChangeStrategy(schemaChanged: false);
        await cache.GetResourcesAsync(_connection, Framework, checkingCallerStages.Add);

        gate.SetResult();
        await generatingCall.WaitAsync(_testTimeout);

        Assert.Equal([DataConnectionSetupStage.CheckingSchema], checkingCallerStages);
        Assert.Equal([DataConnectionSetupStage.Scaffolding], generatingCallerStages);
        Assert.DoesNotContain(_progressEvents, e => e.Stage == DataConnectionSetupStage.CheckingSchema);
    }

    private FileSystemDataConnectionResourcesCache CreateCache()
    {
        var repository = new Mock<IDataConnectionResourcesRepository>();
        repository
            .Setup(r => r.GetAsync(It.IsAny<DataConnection>(), It.IsAny<DotNetFrameworkVersion>()))
            .ReturnsAsync(() => _diskCachedResources);

        return new FileSystemDataConnectionResourcesCache(
            repository.Object,
            _strategyFactory.Object,
            _generatorFactory.Object,
            _eventBus,
            NullLogger<FileSystemDataConnectionResourcesCache>.Instance);
    }

    private void SetupSchemaChangeStrategy(bool schemaChanged)
    {
        var strategy = new Mock<IDataConnectionSchemaChangeDetectionStrategy>();
        strategy
            .Setup(s => s.DidSchemaChangeAsync(It.IsAny<DataConnection>()))
            .ReturnsAsync(schemaChanged);

        _strategyFactory
            .Setup(f => f.GetStrategies(It.IsAny<DataConnection>()))
            .Returns([strategy.Object]);
    }

    private static Func<Action<DataConnectionSetupStage>?, Task> ReportStages(params DataConnectionSetupStage[] stages)
    {
        return reportStage =>
        {
            foreach (var stage in stages)
            {
                reportStage?.Invoke(stage);
            }

            return Task.CompletedTask;
        };
    }

    private static DataConnection CreateDataConnection()
    {
        return new Mock<DataConnection>(Guid.NewGuid(), "TestDb", DataConnectionType.SQLite) { CallBase = true }.Object;
    }

    private sealed class FakeGenerator(Func<Action<DataConnectionSetupStage>?, Task> generate)
        : IDataConnectionResourcesGenerator
    {
        public async Task<DataConnectionResources> GenerateResourcesAsync(
            DataConnection dataConnection,
            DotNetFrameworkVersion targetFrameworkVersion,
            Action<DataConnectionSetupStage>? reportStage = null)
        {
            await generate(reportStage);
            return new DataConnectionResources(dataConnection, DateTime.UtcNow);
        }
    }
}
