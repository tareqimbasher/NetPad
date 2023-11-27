using NetPad.Runtimes;

/// <summary>
/// Meant to be injected into script code so it can initialize <see cref="ScriptRuntimeServices"/>.
/// The class name must be "Program" and must be partial. This is so we augment the base "Program" class
/// .NET will implicitly wrap top-level statements within. Code in the constructor will be called by the runtime
/// before a script's code is executed.
///
/// This is embedded into the assembly to be read later as an Embedded Resource.
/// </summary>
public partial class Program
{
    public static readonly UserScript UserScript = new(new Guid("SCRIPT_ID"), "SCRIPT_NAME", "SCRIPT_LOCATION");

    static Program()
    {
        var args = Environment.GetCommandLineArgs();

        if (args.Contains("-help"))
        {
            ScriptRuntimeServices.UseStandardIO(ExternalProcessOutputFormat.Console, true);

            Console.WriteLine($"# {UserScript.Name}");
            Console.WriteLine("""
                              help:
                                Output Format:
                                    -console    (default)
                                    -text       Text output
                                    -html       HTML output

                                Other Options:
                                    -no-color   Do not color output. Does not apply to "HTML" format.
                              """);

            Environment.Exit(0);
        }

        ExternalProcessOutputFormat format = args.Contains("-html")
            ? ExternalProcessOutputFormat.HTML
            : args.Contains("-text")
                ? ExternalProcessOutputFormat.Text
                : ExternalProcessOutputFormat.Console;

        bool useConsoleColors = !args.Contains("-no-color");

        ScriptRuntimeServices.UseStandardIO(format, useConsoleColors);
    }
}
