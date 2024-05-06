namespace NetPad.Media;

public static class MediaFileExtensions
{
    /// <summary>
    /// Sets the <see cref="MediaFile.DisplayWidth"/> of this media file and returns the same instance.
    /// </summary>
    public static T WithDisplayWidth<T>(this T file, string width) where T : MediaFile
    {
        file.DisplayWidth = width;
        return file;
    }

    /// <summary>
    /// Sets the <see cref="MediaFile.DisplayHeight"/> of this media file and returns the same instance.
    /// </summary>
    public static T WithDisplayHeight<T>(this T file, string height) where T : MediaFile
    {
        file.DisplayHeight = height;
        return file;
    }

    /// <summary>
    /// Sets the <see cref="MediaFile.DisplayWidth"/> and <see cref="MediaFile.DisplayHeight"/> of this media file and returns the same instance.
    /// </summary>
    public static T WithDisplaySize<T>(this T file, string width, string height) where T : MediaFile =>
        file.WithDisplayWidth<T>(width)
            .WithDisplayHeight<T>(height);
}
