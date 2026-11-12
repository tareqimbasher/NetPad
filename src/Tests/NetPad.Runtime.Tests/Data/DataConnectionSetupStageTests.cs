using NetPad.Data;

namespace NetPad.Runtime.Tests.Data;

public class DataConnectionSetupStageTests
{
    public static TheoryData<DataConnectionSetupStage> AllStages => new(Enum.GetValues<DataConnectionSetupStage>());

    [Theory]
    [MemberData(nameof(AllStages))]
    public void EveryStage_HasDescription(DataConnectionSetupStage stage)
    {
        Assert.False(string.IsNullOrWhiteSpace(stage.GetDescription()));
    }
}
