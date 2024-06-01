namespace NetPad.Apps.Data.EntityFrameworkCore.Scaffolding.Transforms;

public interface IScaffoldedModelTransform
{
    void Transform(ScaffoldedDatabaseModel model);
}
