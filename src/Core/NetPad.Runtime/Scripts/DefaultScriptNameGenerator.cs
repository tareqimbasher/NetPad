using NetPad.Sessions;

namespace NetPad.Scripts;

public class DefaultScriptNameGenerator(ISession session) : IScriptNameGenerator
{
    public string Generate(string baseName = "Script")
    {
        var existingScriptNames = session.Environments.Select(e => e.Script.Name).ToHashSet();
        string newName;
        var num = 0;

        do
        {
            newName = $"{baseName} {++num}";
        } while (existingScriptNames.Contains(newName));

        return newName;
    }
}
