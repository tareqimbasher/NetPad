using System;

namespace NetPad.Data.EntityFrameworkCore.Scaffolding;

public record ScaffoldOptions
{
    public bool NoPluralize { get; set; }
    public bool UseDatabaseNames { get; set; }
    public string[] Schemas { get; set; } = Array.Empty<string>();
    public string[] Tables { get; set; } = Array.Empty<string>();
}
