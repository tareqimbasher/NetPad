namespace NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;

// Used to compare if any values have changed.
public record ScaffoldOptions
{
    public bool NoPluralize { get; init; }
    public bool UseDatabaseNames { get; init; }
    public string[] Schemas { get; init; } = [];
    public string[] Tables { get; init; } = [];
    public bool OptimizeDbContext { get; init; }

    public virtual bool Equals(ScaffoldOptions? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return NoPluralize == other.NoPluralize
               && UseDatabaseNames == other.UseDatabaseNames
               && OptimizeDbContext == other.OptimizeDbContext
               && Schemas.SequenceEqual(other.Schemas)
               && Tables.SequenceEqual(other.Tables);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(NoPluralize);
        hash.Add(UseDatabaseNames);
        hash.Add(OptimizeDbContext);
        foreach (var s in Schemas) hash.Add(s);
        foreach (var t in Tables) hash.Add(t);
        return hash.ToHashCode();
    }
}
