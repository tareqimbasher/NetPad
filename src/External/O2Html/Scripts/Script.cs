namespace O2Html.Scripts;

public class Script : IScript
{
    private readonly string _code;

    public Script(string code)
    {
        _code = code;
    }

    public string GetCode() => _code;
}