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
    {0}
}}
";
            
            if (query.Config.QueryKind == QueryKind.Expression)
            {
                throw new NotImplementedException("Expression code parsing is not implemented yet.");
            }
            else if (query.Config.QueryKind == QueryKind.Statements)
            {
                code = $@"
public void Main()
{{
    {queryCode}
}}
";
            }
            else if (query.Config.QueryKind == QueryKind.Program)
            {
                code = queryCode;
            }
            else
            {
                throw new NotImplementedException($"Code parsing is not implemented yet for query kind: {query.Config.QueryKind}");
            }

            return string.Format(program, code);
        }
    }
}