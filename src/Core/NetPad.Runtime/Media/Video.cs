namespace NetPad.Media;

/// <summary>
/// Represents a dumpable video file.
/// </summary>
public class Video : MediaFile<Video>
{
    public Video()
    {
    }

    public Video(string filePath) : base(filePath)
    {
    }

    public Video(Uri uri) : base(uri)
    {
    }
}
