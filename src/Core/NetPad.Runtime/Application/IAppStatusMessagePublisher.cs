namespace NetPad.Application;

/// <summary>
/// Publishes application status message updates mainly to show to users on UI.
/// </summary>
public interface IAppStatusMessagePublisher
{
    /// <summary>
    /// Publishes a message relevant only in the moment (e.g. "Compiling..."); if the user misses it, nothing is lost.
    /// </summary>
    Task PublishTransientAsync(string text, AppStatusMessageSeverity severity = AppStatusMessageSeverity.Info);

    /// <inheritdoc cref="PublishTransientAsync(string,AppStatusMessageSeverity)"/>
    Task PublishTransientAsync(
        Guid scriptId,
        string text,
        AppStatusMessageSeverity severity = AppStatusMessageSeverity.Info);

    /// <summary>
    /// Publishes a noteworthy occurrence kept on record for the user to review later (e.g. "Script finished with an error").
    /// </summary>
    Task PublishNoticeAsync(string text, AppStatusMessageSeverity severity = AppStatusMessageSeverity.Info);

    /// <inheritdoc cref="PublishNoticeAsync(string,AppStatusMessageSeverity)"/>
    Task PublishNoticeAsync(
        Guid scriptId,
        string text,
        AppStatusMessageSeverity severity = AppStatusMessageSeverity.Info);

    /// <summary>
    /// Publishes a message the user must become aware of (e.g. a data-loss warning).
    /// </summary>
    Task PublishAlertAsync(string text, AppStatusMessageSeverity severity = AppStatusMessageSeverity.Info);

    /// <inheritdoc cref="PublishAlertAsync(string,AppStatusMessageSeverity)"/>
    Task PublishAlertAsync(
        Guid scriptId,
        string text,
        AppStatusMessageSeverity severity = AppStatusMessageSeverity.Info);
}
