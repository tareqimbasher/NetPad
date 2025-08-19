using NetPad.Apps.Shells.Electron;
using NetPad.Apps.Shells.Tauri;
using NetPad.Apps.Shells.Web;
using NetPad.Host;

namespace NetPad.Apps.App.Tests;

public class ProgramArgsTests
{
    [Fact]
    public void Preserves_Raw_Args()
    {
        var input = new[] { "--tauri", "--swagger", "--parent-pid", "1234" };
        var args = new ProgramArgs(input);

        Assert.Equal(input, args.Raw);
    }

    [Theory]
    [InlineData("/ELECTRONPORT=9222")]
    [InlineData("--foo /electronport 1234")]
    [InlineData("--something abc /ElectRonPort:5555")]
    public void Determines_ElectronShell_Correctly(string input)
    {
        var args = new ProgramArgs(input.Split(' '));
        Assert.Equal(ShellType.Electron, args.ShellType);

        var shell = args.CreateShell();
        Assert.IsType<ElectronShell>(shell);
    }

    [Theory]
    [InlineData("--tauri")]
    [InlineData("--TAURI")]
    [InlineData("xx --tauri yy")]
    public void Determines_TauriShell_Correctly(string input)
    {
        var args = new ProgramArgs(input.Split(' '));
        Assert.Equal(ShellType.Tauri, args.ShellType);

        var shell = args.CreateShell();
        Assert.IsType<TauriShell>(shell);
    }

    [Theory]
    [InlineData("--foo --bar")]
    [InlineData("")]
    [InlineData("--swagger")]
    public void Defaults_To_WebShell(string input)
    {
        var args = new ProgramArgs(input.Split(' '));
        Assert.Equal(ShellType.Web, args.ShellType);

        var shell = args.CreateShell();
        Assert.IsType<WebBrowserShell>(shell);
    }

    [Theory]
    [InlineData("", RunMode.Normal)]
    [InlineData("--foo", RunMode.Normal)]
    [InlineData("--swagger", RunMode.SwaggerGen)]
    public void Determines_RunMode_Correctly(string input, RunMode expectedRunMode)
    {
        var args = new ProgramArgs(input.Split(' '));
        Assert.Equal(expectedRunMode, args.RunMode);
    }

    [Theory]
    [InlineData(new[] { "--parent-pid", "42" }, 42)]
    [InlineData(new[] { "--tauri", "--parent-pid", "12345" }, 12345)]
    public void ParentPid_Parses_When_Valid_Integer(string[] input, int expectedPid)
    {
        var args = new ProgramArgs(input);
        Assert.Equal(expectedPid, args.ParentPid);
    }

    [Theory]
    [InlineData("--parent-pid")] // missing value
    [InlineData("--tauri")] // not provided at all
    public void ParentPid_Is_Null_When_Not_Provided(string input)
    {
        var args = new ProgramArgs(input.Split(' '));
        Assert.Null(args.ParentPid);
    }
}
