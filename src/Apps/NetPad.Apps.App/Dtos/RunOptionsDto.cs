using System;
using System.Collections.Generic;
using System.Linq;
using NetPad.Compilation;
using NetPad.Runtimes;

namespace NetPad.Dtos;

public class RunOptionsDto
{
    public string? SpecificCodeToRun { get; set; }
    public SourceCodeDto[]? AdditionalCode { get; set; }

    public RunOptions ToRunOptions()
    {
        AdditionalCode ??= Array.Empty<SourceCodeDto>();

        var runOptions = new RunOptions()
        {
            SpecificCodeToRun = SpecificCodeToRun
        };

        runOptions.AdditionalCode.AddRange(AdditionalCode.Select(c => new SourceCode(c.Code, c.Namespaces)));

        return runOptions;
    }

    public class SourceCodeDto
    {
        public HashSet<string> Namespaces { get; set; }
        public string? Code { get; set; }
    }
}
