namespace NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;

// Should be a record. Used to compare if any values have changed.
public record ScaffoldOptions
{
    public bool NoPluralize { get; set; }
    public bool UseDatabaseNames { get; set; }
    public string[] Schemas { get; set; } = [];
    public string[] Tables { get; set; } = [];
    public bool OptimizeDbContext { get; set; }
}
