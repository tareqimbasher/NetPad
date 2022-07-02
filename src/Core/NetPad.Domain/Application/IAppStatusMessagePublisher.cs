using System.Threading.Tasks;

namespace NetPad.Application;

public interface IAppStatusMessagePublisher
{
    /// <summary>
    /// Publishes a <see cref="AppStatusMessage"/>.
    /// </summary>
    /// <param name="text">The text of the message.</param>
    /// <param name="priority">The priority of this status message.</param>
    /// <param name="persistant">Whether the message should persist or clear out after a timeout.</param>
    Task PublishAsync(string text, AppStatusMessagePriority priority = AppStatusMessagePriority.Normal, bool persistant = false);
}
