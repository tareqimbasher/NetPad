using NetPad.Sessions;

namespace NetPad.Scripts;

public class DefaultScriptNameGenerator : IScriptNameGenerator
{
    private readonly ISession _session;

    public DefaultScriptNameGenerator(ISession session)
    {
        _session = session;
    }

    public string Generate(string baseName = "Script")
    {
        var existingScriptNames = _session.Environments.Select(e => e.Script.Name).ToHashSet();
        string newName;
        var num = 0;

        do
        {
            newName = $"{baseName} {++num}";
        } while (existingScriptNames.Contains(newName));

        return newName;
    }
}
