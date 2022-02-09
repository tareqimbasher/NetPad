namespace O2Html.Styles;

public class StyleSheet : IStyleSheet
{
    private readonly string _code;

    public StyleSheet(string code)
    {
        _code = code;
    }

    public string GetCode() => _code;
}