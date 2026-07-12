using NetPad.Application.Events;
using NetPad.Events;

namespace NetPad.Application;

public class AppStatusMessagePublisher(IEventBus eventBus) : IAppStatusMessagePublisher
{
    public Task PublishTransientAsync(string text, AppStatusMessageSeverity severity = AppStatusMessageSeverity.Info) =>
        PublishAsync(new AppStatusMessage(text, AppStatusMessageKind.Transient, severity));

    public Task PublishTransientAsync(
        Guid scriptId,
        string text,
        AppStatusMessageSeverity severity = AppStatusMessageSeverity.Info) =>
        PublishAsync(new AppStatusMessage(scriptId, text, AppStatusMessageKind.Transient, severity));

    public Task PublishNoticeAsync(string text, AppStatusMessageSeverity severity = AppStatusMessageSeverity.Info) =>
        PublishAsync(new AppStatusMessage(text, AppStatusMessageKind.Notice, severity));

    public Task PublishNoticeAsync(
        Guid scriptId,
        string text,
        AppStatusMessageSeverity severity = AppStatusMessageSeverity.Info) =>
        PublishAsync(new AppStatusMessage(scriptId, text, AppStatusMessageKind.Notice, severity));

    public Task PublishAlertAsync(
        string text,
        AppStatusMessageSeverity severity = AppStatusMessageSeverity.Info) =>
        PublishAsync(new AppStatusMessage(text, AppStatusMessageKind.Alert, severity));

    public Task PublishAlertAsync(
        Guid scriptId,
        string text,
        AppStatusMessageSeverity severity = AppStatusMessageSeverity.Info) =>
        PublishAsync(new AppStatusMessage(scriptId, text, AppStatusMessageKind.Alert, severity));

    private Task PublishAsync(AppStatusMessage message) =>
        eventBus.PublishAsync(new AppStatusMessagePublishedEvent(message));
}
