using System.Diagnostics.CodeAnalysis;
using NetPad.Configuration;
using NetPad.DotNet;
using NetPad.Utilities;
using OmniSharp;
using OmniSharp.Stdio;

namespace NetPad.Plugins.OmniSharp.Services;

public class OmniSharpServerBuilder
{
    private readonly ILogger<OmniSharpServerBuilder> _logger;
    private readonly IOmniSharpServerFactory _omniSharpServerFactory;
    private readonly IOmniSharpServerLocator _omniSharpServerLocator;
    private readonly Settings _settings;
    private readonly IDotNetInfo _dotNetInfo;

    public OmniSharpServerBuilder(
        ILogger<OmniSharpServerBuilder> logger,
        IOmniSharpServerFactory omniSharpServerFactory,
        IOmniSharpServerLocator omniSharpServerLocator,
        Settings settings,
        IDotNetInfo dotNetInfo)
    {
        _logger = logger;
        _omniSharpServerFactory = omniSharpServerFactory;
        _omniSharpServerLocator = omniSharpServerLocator;
        _settings = settings;
        _dotNetInfo = dotNetInfo;
    }

    private static string GetArgs(OmniSharpServerLocation location, string args)
    {
        return location switch
        {
            { EntryDllPath.Length: > 0 } => $"exec {location.EntryDllPath} {args}",
            _ => args,
        };
    }

    private string GetExecutable(OmniSharpServerLocation location)
    {
        return location switch
        {
            { EntryDllPath.Length: > 0 } => _dotNetInfo.LocateDotNetExecutable()!,
            _ => location.ExecutablePath!,
        };
    }

    private string GetOmniSharpServerArguments()
    {
        return new[]
        {
            $"--hostPID {Environment.ProcessId}",
            "--encoding utf-8",
            "--loglevel Information",
            //"-z",

            "Sdk:IncludePrereleases=true",

            "FileOptions:SystemExcludeSearchPatterns:0=**/.git",
            "FileOptions:SystemExcludeSearchPatterns:1=**/.svn",
            "FileOptions:SystemExcludeSearchPatterns:2=**/.hg",
            "FileOptions:SystemExcludeSearchPatterns:3=**/CVS",
            "FileOptions:SystemExcludeSearchPatterns:4=**/.DS_Store",
            "FileOptions:SystemExcludeSearchPatterns:5=**/Thumbs.db",
            $"RoslynExtensionsOptions:EnableAnalyzersSupport={_settings.OmniSharp.EnableAnalyzersSupport}",
            "RoslynExtensionsOptions:EnableEditorConfigSupport=false",
            "RoslynExtensionsOptions:EnableDecompilationSupport=true",
            $"RoslynExtensionsOptions:EnableImportCompletion={_settings.OmniSharp.EnableImportCompletion}",
            "RoslynExtensionsOptions:EnableAsyncCompletion=false",

            $"RoslynExtensionsOptions:InlayHintsOptions:EnableForParameters={_settings.OmniSharp.InlayHints.EnableParameters}",
            $"RoslynExtensionsOptions:InlayHintsOptions:ForLiteralParameters={_settings.OmniSharp.InlayHints.EnableLiteralParameters}",
            $"RoslynExtensionsOptions:InlayHintsOptions:ForIndexerParameters={_settings.OmniSharp.InlayHints.EnableIndexerParameters}",
            $"RoslynExtensionsOptions:InlayHintsOptions:ForObjectCreationParameters={_settings.OmniSharp.InlayHints.EnableObjectCreationParameters}",
            $"RoslynExtensionsOptions:InlayHintsOptions:ForOtherParameters={_settings.OmniSharp.InlayHints.EnableOtherParameters}",
            $"RoslynExtensionsOptions:InlayHintsOptions:SuppressForParametersThatDifferOnlyBySuffix={_settings.OmniSharp.InlayHints.SuppressForParametersThatDifferOnlyBySuffix}",
            $"RoslynExtensionsOptions:InlayHintsOptions:SuppressForParametersThatMatchMethodIntent={_settings.OmniSharp.InlayHints.SuppressForParametersThatMatchMethodIntent}",
            $"RoslynExtensionsOptions:InlayHintsOptions:SuppressForParametersThatMatchArgumentName={_settings.OmniSharp.InlayHints.SuppressForParametersThatMatchArgumentName}",
            $"RoslynExtensionsOptions:InlayHintsOptions:EnableForTypes={_settings.OmniSharp.InlayHints.EnableTypes}",
            $"RoslynExtensionsOptions:InlayHintsOptions:ForImplicitVariableTypes={_settings.OmniSharp.InlayHints.EnableImplicitVariableTypes}",
            $"RoslynExtensionsOptions:InlayHintsOptions:ForLambdaParameterTypes={_settings.OmniSharp.InlayHints.EnableLambdaParameterTypes}",
            $"RoslynExtensionsOptions:InlayHintsOptions:ForImplicitObjectCreation={_settings.OmniSharp.InlayHints.EnableImplicitObjectCreation}"
        }.JoinToString(" ");
    }

    public async Task<IOmniSharpStdioServer> BuildAsync(OmniSharpProject project)
    {
        var args = GetOmniSharpServerArguments();
        var location = await _omniSharpServerLocator.GetServerLocationAsync();
        if (location is null || !location.Verify())
        {
            throw new Exception("OmniSharp server executable path is invalid!");
        }

        var executable = GetExecutable(location);
        var fullArgs = GetArgs(location, args);
        var server = _omniSharpServerFactory.CreateStdioServerFromNewProcess(
            executable,
            project.ProjectDirectoryPath,
            fullArgs,
            _dotNetInfo.LocateDotNetRootDirectory());

        _logger.LogInformation("Prepare omnisharp server\nExecutable path: {EntryPath}\nProject dir: {ProjDirPath}\nWith args: {Args}",
            executable,
            project.ProjectDirectoryPath,
            fullArgs);

        return server;
    }

}
