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

public class NetPad_Query_Program
{{
    public Exception? Exception {{ get; private set; }}

    public void Main()
    {{
        try
        {{
            new UserQuery_Program().Main();
        }}
        catch (Exception ex)
        {{
            Exception = ex;
        }}
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

            return string.Format(program, code);
        }
    }
}
