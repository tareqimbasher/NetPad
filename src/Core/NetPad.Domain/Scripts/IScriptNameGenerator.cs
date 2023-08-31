namespace NetPad.Scripts;

public interface IScriptNameGenerator
{
    string Generate(string baseName = "Script");
}
