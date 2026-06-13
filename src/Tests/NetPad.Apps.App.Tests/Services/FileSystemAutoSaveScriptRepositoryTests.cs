using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NetPad.Application;
using NetPad.Apps.Scripts;
using NetPad.Common;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Scripts;
using NetPad.Services;

namespace NetPad.Apps.App.Tests.Services;

public sealed class FileSystemAutoSaveScriptRepositoryTests : IDisposable
{
    private readonly string _testRoot;
    private readonly string _scriptsDir;
    private readonly string _autoSaveDir;
    private readonly string _indexPath;
    private readonly Settings _settings;
    private readonly Mock<IScriptRepository> _scriptRepository;
    private readonly Mock<IScriptNameGenerator> _scriptNameGenerator;
    private readonly Mock<IDataConnectionRepository> _dataConnectionRepository;
    private readonly Mock<IDotNetInfo> _dotNetInfo;
    private readonly Mock<IAppStatusMessagePublisher> _appStatusMessagePublisher;

    public FileSystemAutoSaveScriptRepositoryTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "netpad-tests", Guid.NewGuid().ToString("N"));
        _scriptsDir = Path.Combine(_testRoot, "scripts");
        _autoSaveDir = Path.Combine(_testRoot, "autosave");
        _indexPath = Path.Combine(_autoSaveDir, "index.json");
        Directory.CreateDirectory(_scriptsDir);
        Directory.CreateDirectory(_autoSaveDir);

        _settings = new Settings();
        typeof(Settings).GetProperty(nameof(Settings.ScriptsDirectoryPath))!
            .SetValue(_settings, _scriptsDir);
        typeof(Settings).GetProperty(nameof(Settings.AutoSaveScriptsDirectoryPath))!
            .SetValue(_settings, _autoSaveDir);

        _scriptRepository = new Mock<IScriptRepository>();
        _scriptNameGenerator = new Mock<IScriptNameGenerator>();
        _dataConnectionRepository = new Mock<IDataConnectionRepository>();
        _dotNetInfo = new Mock<IDotNetInfo>();
        _appStatusMessagePublisher = new Mock<IAppStatusMessagePublisher>();

        _scriptNameGenerator.Setup(g => g.Generate(It.IsAny<string>())).Returns("Generated");
    }

    [Fact]
    public async Task SaveAsync_Writes_Path_To_Index()
    {
        var repo = CreateRepository();
        var script = CreateScript();
        script.SetPath("/tmp/myscript.netpad");

        await repo.SaveAsync(script);

        var entry = ReadIndexEntry(script.Id);
        Assert.NotNull(entry);
        Assert.Equal("/tmp/myscript.netpad", (string?)entry!["originalPath"]);
        Assert.Equal(script.Name, (string?)entry["name"]);
    }

    [Fact]
    public async Task SaveAsync_Writes_Null_Path_For_New_Script()
    {
        var repo = CreateRepository();
        var script = CreateScript();

        await repo.SaveAsync(script);

        var entry = ReadIndexEntry(script.Id);
        Assert.NotNull(entry);
        Assert.Null((string?)entry!["originalPath"]);
    }

    [Fact]
    public async Task GetScriptAsync_Restores_Path_When_Original_File_Exists()
    {
        var originalPath = Path.Combine(_testRoot, "external.netpad");
        File.WriteAllText(originalPath, "placeholder");

        var repo = CreateRepository();
        var script = CreateScript();
        script.SetPath(originalPath);
        await repo.SaveAsync(script);

        // Simulate restart: script not in main scripts repo
        _scriptRepository.Setup(r => r.GetAsync(script.Id)).ReturnsAsync((Script?)null);

        var restored = await repo.GetScriptAsync(script.Id);

        Assert.NotNull(restored);
        Assert.Equal(originalPath.Replace('\\', '/'), restored!.Path);
        _appStatusMessagePublisher.Verify(
            p => p.PublishAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<AppStatusMessagePriority>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task GetScriptAsync_Leaves_Path_Null_And_Publishes_Status_When_Original_File_Missing()
    {
        var originalPath = Path.Combine(_testRoot, "missing.netpad");

        var repo = CreateRepository();
        var script = CreateScript();
        // Pretend the script was at originalPath but the file got deleted
        script.SetPath(originalPath);
        await repo.SaveAsync(script);
        // Make sure no file lingers at that path
        if (File.Exists(originalPath)) File.Delete(originalPath);

        _scriptRepository.Setup(r => r.GetAsync(script.Id)).ReturnsAsync((Script?)null);

        var restored = await repo.GetScriptAsync(script.Id);

        Assert.NotNull(restored);
        Assert.Null(restored!.Path);
        _appStatusMessagePublisher.Verify(
            p => p.PublishAsync(restored.Id, It.Is<string>(s => s.Contains("missing", StringComparison.OrdinalIgnoreCase)), It.IsAny<AppStatusMessagePriority>(), It.IsAny<bool>()),
            Times.Once);
    }

    [Fact]
    public async Task GetScriptAsync_Migrates_Legacy_Index_Format_And_Persists_New_Shape_On_Next_Save()
    {
        var script = CreateScript();

        // Write a script file directly + a legacy index (Dictionary<Guid, string>)
        var scriptFilePath = Path.Combine(_autoSaveDir, $"{script.Id}.netpad");
        File.WriteAllText(scriptFilePath, ScriptSerializer.Serialize(script));

        var legacyIndex = new Dictionary<Guid, string> { { script.Id, "LegacyName" } };
        File.WriteAllText(_indexPath, JsonSerializer.Serialize(legacyIndex, true));

        _scriptRepository.Setup(r => r.GetAsync(script.Id)).ReturnsAsync((Script?)null);

        var repo = CreateRepository();

        // Read should succeed with the legacy index in place
        var restored = await repo.GetScriptAsync(script.Id);
        Assert.NotNull(restored);
        Assert.Equal("LegacyName", restored!.Name);
        Assert.Null(restored.Path);

        // Saving back should rewrite the index in the new shape (versioned, with "entries" wrapper)
        await repo.SaveAsync(restored);
        Assert.Equal(1, ReadIndexVersion());
        var entry = ReadIndexEntry(script.Id);
        Assert.NotNull(entry);
        Assert.Equal("LegacyName", (string?)entry!["name"]);
    }

    [Fact]
    public async Task SaveAsync_Writes_File_With_Version_Header()
    {
        var repo = CreateRepository();
        var script = CreateScript();

        await repo.SaveAsync(script);

        Assert.Equal(1, ReadIndexVersion());
    }

    /// <summary>
    /// Reads a single entry from the on-disk index. The v1 file shape is
    /// <c>{"version": 1, "entries": {"&lt;guid&gt;": {"name": "...", "originalPath": "..."}}}</c>.
    /// </summary>
    private JsonObject? ReadIndexEntry(Guid scriptId)
    {
        var json = File.ReadAllText(_indexPath);
        var root = JsonNode.Parse(json) as JsonObject;
        var entries = root?["entries"] as JsonObject;
        return entries?[scriptId.ToString()] as JsonObject;
    }

    private int ReadIndexVersion()
    {
        var json = File.ReadAllText(_indexPath);
        var root = JsonNode.Parse(json) as JsonObject;
        return (int?)root?["version"] ?? -1;
    }

    private FileSystemAutoSaveScriptRepository CreateRepository() =>
        new(
            _settings,
            _scriptRepository.Object,
            _scriptNameGenerator.Object,
            _dataConnectionRepository.Object,
            _dotNetInfo.Object,
            _appStatusMessagePublisher.Object,
            new NullLogger<FileSystemAutoSaveScriptRepository>());

    private static Script CreateScript()
    {
        return new Script(
            Guid.NewGuid(),
            "TestScript",
            new ScriptConfig(ScriptKind.Program, GlobalConsts.AppDotNetFrameworkVersion),
            "Console.WriteLine(\"hi\");");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testRoot))
            {
                Directory.Delete(_testRoot, recursive: true);
            }
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
