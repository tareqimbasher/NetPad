namespace NetPad.Apps.CQs;

public class OpenWindowCommand(string windowName) : Command
{
    public string WindowName { get; } = windowName;
    public WindowOptions Options { get; } = new();
    public Dictionary<string, object?> Metadata { get; } = new();

    public class WindowOptions
    {
        /// <summary>
        /// The height of the window. If value is between 0 and 1, the window height will be
        /// the height of the screen multiplied by this value.
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// The width of the window. If value is between 0 and 1, the window width will be
        /// the width of the screen multiplied by this value.
        /// </summary>
        public double Width { get; set; }
    }
}
