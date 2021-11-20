using System;
using NetPad.Queries;

namespace NetPad.Runtimes
{
    public class CodeParser
    {
        public static string GetQueryCode(Query query)
        {
            string queryCode = query.Code;
            string code;

            string program = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using NetPad.Runtimes;

public static class UserQuery
{{
    private static IQueryRuntimeOutputWriter OutputWriter {{ get; set; }}
    private static Exception? Exception {{ get; set; }}

    private static void Main(IQueryRuntimeOutputWriter outputWriter)
    {{
        OutputWriter = outputWriter;

        try
        {{
            new UserQuery_Program().Main();
        }}
        catch (Exception ex)
        {{
            Exception = ex;
        }}
    }}

    public static void ConsoleWrite(object? o)
    {{
        OutputWriter.WriteAsync(o?.ToString());
    }}

    public static void ConsoleWriteLine(object? o)
    {{
        OutputWriter.WriteAsync(o?.ToString() + ""\n"");
    }}
}}

public class UserQuery_Program
{{
    {0}
}}
";

            if (query.Config.Kind == QueryKind.Expression)
            {
                throw new NotImplementedException("Expression code parsing is not implemented yet.");
            }
            else if (query.Config.Kind == QueryKind.Statements)
            {
                code = $@"
public void Main()
{{
    {queryCode}
}}
";
            }
            else if (query.Config.Kind == QueryKind.Program)
            {
                code = queryCode;
            }
            else
            {
                throw new NotImplementedException($"Code parsing is not implemented yet for query kind: {query.Config.Kind}");
            }

            code = string.Format(program, code);
            return code
                .Replace("Console.WriteLine", "UserQuery.ConsoleWriteLine")
                .Replace("Console.Write", "UserQuery.ConsoleWrite");
        }
    }
}
