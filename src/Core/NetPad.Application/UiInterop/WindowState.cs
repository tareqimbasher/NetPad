namespace NetPad.UiInterop;

public class WindowState
{
    public WindowState(WindowViewStatus viewStatus, bool isAlwaysOnTop)
    {
        ViewStatus = viewStatus;
        IsAlwaysOnTop = isAlwaysOnTop;
    }

    public WindowViewStatus ViewStatus { get; }
    public bool IsAlwaysOnTop { get; }
    public bool IsMinimized => ViewStatus == WindowViewStatus.Minimized;
    public bool IsMaximized => ViewStatus == WindowViewStatus.Maximized;
}
