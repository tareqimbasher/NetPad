using Moq;
using NetPad.Apps.CQs;
using NetPad.Configuration;
using NetPad.Events;

namespace NetPad.Apps.Common.Tests.CQs;

public class UpdateSettingsCommandHandlerTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Saves_The_Code_Tooling_Language(bool useEnglish)
    {
        var tempDir = Path.GetTempPath();
        var settings = new Settings(tempDir, tempDir);
        settings.Localization.SetUseEnglishForCodeTooling(!useEnglish);
        var incoming = new Settings(tempDir, tempDir);
        incoming.Localization.SetUseEnglishForCodeTooling(useEnglish);
        var repository = new Mock<ISettingsRepository>();

        var handler = new UpdateSettingsCommand.Handler(settings, repository.Object, Mock.Of<IEventBus>());

        await handler.Handle(new UpdateSettingsCommand(incoming), CancellationToken.None);

        Assert.Equal(useEnglish, settings.Localization.UseEnglishForCodeTooling);
        repository.Verify(r => r.SaveSettingsAsync(settings, false), Times.Once);
    }
}
