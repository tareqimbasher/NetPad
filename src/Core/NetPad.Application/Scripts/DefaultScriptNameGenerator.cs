using NetPad.Sessions;

namespace NetPad.Scripts;

public class DefaultScriptNameGenerator : IScriptNameGenerator
{
    private readonly ISession _session;

    public DefaultScriptNameGenerator(ISession session)
    {
        _session = session;
    }

    public string Generate()
    {
        const string baseName = "Script";
        int number = 1;

        while (_session.Environments.Any(m => m.Script.Name == $"{baseName} {number}"))
        {
            number++;
        }

        return $"{baseName} {number}";
    }
}
