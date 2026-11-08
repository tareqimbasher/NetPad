using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NetPad.Apps.Data;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.PostgreSql;
using NetPad.IO;

namespace NetPad.Apps.Common.Tests.Data;

public sealed class FileSystemDataConnectionRepositoryTests : IDisposable
{
    private readonly DirectoryPath _directory;
    private readonly FilePath _connectionsFile;

    public FileSystemDataConnectionRepositoryTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "NetPadTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_directory.Path);
        _connectionsFile = _directory.CombineFilePath("data-connections.json");
    }

    public void Dispose()
    {
        Directory.Delete(_directory.Path, true);
    }

    [Fact]
    public async Task An_Unparseable_File_Is_Backed_Up_Byte_For_Byte_And_Reset_To_Empty()
    {
        byte[] corrupt = [0x7B, 0x20, 0xFF, 0xFE, 0x00, 0x6E, 0x6F, 0x74, 0x0A];
        await File.WriteAllBytesAsync(_connectionsFile.Path, corrupt);

        var connections = await CreateRepository().GetAllAsync();

        Assert.Empty(connections);
        Assert.Equal(corrupt, await File.ReadAllBytesAsync(Assert.Single(Backups())));
        await AssertFileIsEmptyV1();
    }

    [Fact]
    public async Task An_Empty_File_Is_Backed_Up_And_Reset_To_Empty()
    {
        await File.WriteAllBytesAsync(_connectionsFile.Path, []);

        var connections = await CreateRepository().GetAllAsync();

        Assert.Empty(connections);
        Assert.Empty(await File.ReadAllBytesAsync(Assert.Single(Backups())));
        await AssertFileIsEmptyV1();
    }

    [Fact]
    public async Task A_Second_Load_After_A_Reset_Makes_No_Second_Backup()
    {
        await File.WriteAllTextAsync(_connectionsFile.Path, "{ this is not json");
        var repository = CreateRepository();
        await repository.GetAllAsync();

        // Backup names only resolve to the second, so a second backup could replace the first unseen.
        File.Delete(Assert.Single(Backups()));

        await repository.GetAllAsync();

        Assert.Empty(Backups());
    }

    [Fact]
    public async Task A_Root_That_Is_Not_An_Object_Is_Backed_Up_And_Reset_To_Empty()
    {
        await File.WriteAllTextAsync(_connectionsFile.Path, "[]");

        var connections = await CreateRepository().GetAllAsync();

        Assert.Empty(connections);
        Assert.Equal("[]", await File.ReadAllTextAsync(Assert.Single(Backups())));
        await AssertFileIsEmptyV1();
    }

    [Fact]
    public async Task A_File_From_A_Newer_Version_Throws_And_Is_Left_Unchanged()
    {
        const string newer = """{"version": 99, "connections": []}""";
        await File.WriteAllTextAsync(_connectionsFile.Path, newer);

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateRepository().GetAllAsync());

        Assert.Equal(newer, await File.ReadAllTextAsync(_connectionsFile.Path));
        Assert.Empty(Backups());
    }

    [Fact]
    public async Task Well_Formed_Json_Of_The_Wrong_Shape_Throws_And_Is_Left_Unchanged()
    {
        const string wrongShape = """{"version": 1, "connections": "not a list"}""";
        await File.WriteAllTextAsync(_connectionsFile.Path, wrongShape);

        await Assert.ThrowsAsync<JsonException>(() => CreateRepository().GetAllAsync());

        Assert.Equal(wrongShape, await File.ReadAllTextAsync(_connectionsFile.Path));
        Assert.Empty(Backups());
    }

    [Fact]
    public async Task A_Save_Leaves_No_Temp_File_Behind()
    {
        await CreateRepository().SaveAsync(NewConnection("one"));

        Assert.Equal(_connectionsFile.Path, Assert.Single(Directory.GetFiles(_directory.Path)));
    }

    [Fact]
    public async Task Concurrent_Saves_Of_Different_Connections_All_Survive()
    {
        var repository = CreateRepository();
        var connections = Enumerable.Range(0, 64).Select(i => NewConnection($"connection {i}")).ToArray();

        await Task.WhenAll(connections.Select(c => Task.Run(() => repository.SaveAsync(c))));

        var saved = await CreateRepository().GetAllAsync();
        Assert.Equal(connections.Select(c => c.Id).Order(), saved.Select(c => c.Id).Order());
    }

    [Fact]
    public async Task Reads_While_Saves_Run_Never_Trigger_A_Reset()
    {
        var writer = CreateRepository();
        await writer.SaveAsync(NewConnection("first"));

        // A second instance has its own lock, so it reads the file the way another NetPad process
        // would. Windows refuses to replace a file another handle has open, so it can only run elsewhere.
        var readers = OperatingSystem.IsWindows() ? [writer] : new[] { writer, CreateRepository() };

        using var savesFinished = new CancellationTokenSource();
        var readerTasks = readers.SelectMany(reader => Enumerable.Range(0, 4).Select(_ => Task.Run(async () =>
        {
            while (!savesFinished.IsCancellationRequested)
            {
                await reader.GetAllAsync();
            }
        }))).ToArray();

        try
        {
            for (var i = 0; i < 300; i++)
            {
                await writer.SaveAsync(NewConnection($"connection {i}"));
            }
        }
        finally
        {
            await savesFinished.CancelAsync();
        }

        await Task.WhenAll(readerTasks);

        Assert.False(_directory.Combine("Backups").Exists());
        Assert.Equal(301, (await writer.GetAllAsync()).Count());
    }

    private FileSystemDataConnectionRepository CreateRepository() =>
        new(NullLogger<FileSystemDataConnectionRepository>.Instance, _connectionsFile);

    private static PostgreSqlDatabaseConnection NewConnection(string name) => new(Guid.NewGuid(), name);

    private async Task AssertFileIsEmptyV1()
    {
        var onDisk = await File.ReadAllTextAsync(_connectionsFile.Path);
        Assert.Contains("\"version\": 1", onDisk);
        Assert.Empty(await CreateRepository().GetAllAsync());
    }

    private string[] Backups()
    {
        var backupDirectory = _directory.Combine("Backups");
        return backupDirectory.Exists() ? Directory.GetFiles(backupDirectory.Path) : [];
    }
}
