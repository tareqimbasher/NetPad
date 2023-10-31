using Xunit;

namespace NetPad.Application.Tests;

public class ApplicationInfoTests
{
    [Theory]
    [InlineData(ApplicationType.Electron)]
    public void SetsApplicationTypeCorrectly(ApplicationType applicationType)
    {
        var appInfo = new ApplicationInfo(applicationType);

        Assert.Equal(applicationType, appInfo.Type);
    }
}
