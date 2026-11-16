using System.Globalization;
using NetPad.Configuration;

namespace NetPad.Apps.ProcessSetup;

/// <summary>
/// The language that code tooling (compiler messages, quick fixes, signature help) uses.
/// </summary>
public class CodeToolingLanguage(Settings settings) : IProcessSetupStep
{
    public const string EnglishCultureName = "en";

    private readonly bool _englishRequested = settings.Localization.UseEnglishForCodeTooling;

    /// <summary>
    /// Whether this process uses English for code tooling instead of the system language. Stays false
    /// until <see cref="ApplyToCurrentProcess"/> has run.
    /// </summary>
    public bool UsesEnglish { get; private set; }

    /// <summary>
    /// Makes this process, and the dotnet CLI processes it starts, use the chosen language.
    /// </summary>
    public void ApplyToCurrentProcess()
    {
        if (!_englishRequested)
        {
            return;
        }

        var english = CultureInfo.GetCultureInfo(EnglishCultureName);
        CultureInfo.DefaultThreadCurrentUICulture = english;
        CultureInfo.CurrentUICulture = english;

        // The dotnet CLI and MSBuild read this instead of the UI culture of the process that starts them.
        Environment.SetEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", EnglishCultureName);

        UsesEnglish = true;
    }
}
