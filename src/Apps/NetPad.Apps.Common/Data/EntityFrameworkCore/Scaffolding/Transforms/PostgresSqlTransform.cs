namespace NetPad.Apps.Data.EntityFrameworkCore.Scaffolding.Transforms;

public class PostgresSqlTransform : IScaffoldedModelTransform
{
    public void Transform(ScaffoldedDatabaseModel model)
    {
        // EF Core tools does not add this namespace and is sometimes needed.
        // https://github.com/npgsql/efcore.pg/issues/1613
        model.DbContextFile.AddUsing("Npgsql.EntityFrameworkCore.PostgreSQL.Metadata");
    }
}
