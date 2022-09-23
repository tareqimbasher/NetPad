namespace NetPad.Data.EntityFrameworkCore.Scaffolding.Transforms;

public interface IScaffoldedModelTransform
{
    void Transform(ScaffoldedDatabaseModel model);
}
