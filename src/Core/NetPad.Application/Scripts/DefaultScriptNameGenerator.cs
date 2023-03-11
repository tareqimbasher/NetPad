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

        var lastNumber = _session.Environments
            .Select(e =>
            {
                var nameParts = e.Script.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (nameParts.Length != 2 || nameParts[0] != baseName || !int.TryParse(nameParts[1], out int lastNumber))
                    return null;

                return (int?)lastNumber;
            })
            .Where(i => i > 0)
            .MaxBy(i => i);

        int number = lastNumber == null ? 1 : (lastNumber.Value + 1);

        return $"{baseName} {number}";
    }
}
