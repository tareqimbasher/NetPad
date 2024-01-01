namespace NetPad.Media;

/// <summary>
/// Represents a dumpable image file.
/// </summary>
public class Image : MediaFile<Image>
{
    public Image()
    {
    }

    public Image(string filePath) : base(filePath)
    {
    }

    public Image(Uri uri) : base(uri)
    {
    }
}
