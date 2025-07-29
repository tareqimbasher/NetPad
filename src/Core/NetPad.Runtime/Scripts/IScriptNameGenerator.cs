namespace NetPad.Scripts;

/// <summary>
/// Generates names for newly created scripts.
/// </summary>
public interface IScriptNameGenerator
{
    string Generate(string baseName = "Script");
}
