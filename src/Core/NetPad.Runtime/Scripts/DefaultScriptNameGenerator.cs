using NetPad.Sessions;

namespace NetPad.Scripts;

public class DefaultScriptNameGenerator(ISession session) : IScriptNameGenerator
{
    public string Generate(string baseName = "Script")
    {
        var max = session.GetOpened()
                .Where(e => e.Script.Name.StartsWith($"{baseName} "))
                .Select(e =>
                {
                    var suffix = e.Script.Name[(baseName.Length + 1)..].Trim();
                    var parts = suffix.Split(' ');

                    // Suffix must be a number (can be "11" but can't be "1 1")
                    if (parts.Length == 1 && int.TryParse(parts[0], out var num))
                    {
                        return num;
                    }

                    return 0;
                })
                .DefaultIfEmpty(0)
                .Max();

        return $"{baseName} {max + 1}";
    }
}
