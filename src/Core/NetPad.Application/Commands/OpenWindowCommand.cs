namespace NetPad.Commands;

public class OpenWindowCommand
{
    public OpenWindowCommand(string windowName)
    {
        WindowName = windowName;
        Options = new WindowOptions();
        Metadata = new Dictionary<string, object?>();
    }

    public string WindowName { get; }
    public WindowOptions Options { get; }
    public Dictionary<string, object?> Metadata { get; }

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
