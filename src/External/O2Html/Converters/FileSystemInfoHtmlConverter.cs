using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace O2Html.Converters;

public class FileSystemInfoHtmlConverter : ObjectHtmlConverter
{
    public override bool CanConvert(Type type)
    {
        return typeof(FileSystemInfo).IsAssignableFrom(type);
    }

    private static readonly HashSet<string> _serializableProperties = new()
    {
        nameof(FileSystemInfo.Attributes),
        nameof(FileSystemInfo.FullName),
        nameof(FileSystemInfo.Name),
        nameof(FileSystemInfo.Extension),
        nameof(FileSystemInfo.Exists),
        nameof(FileSystemInfo.CreationTime),
        nameof(FileSystemInfo.CreationTimeUtc),
        nameof(FileSystemInfo.LastAccessTime),
        nameof(FileSystemInfo.LastAccessTimeUtc),
        nameof(FileSystemInfo.LastWriteTime),
        nameof(FileSystemInfo.LastWriteTimeUtc),
#if NET6_0_OR_GREATER
        nameof(FileSystemInfo.LinkTarget),
#endif
#if NET7_0_OR_GREATER
        nameof(FileSystemInfo.UnixFileMode),
#endif
        nameof(DirectoryInfo.Parent),
        nameof(FileInfo.Directory),
    };

    protected override PropertyInfo[] GetReadableProperties(HtmlSerializer htmlSerializer, Type type)
    {
        return base.GetReadableProperties(htmlSerializer, type)
            .Where(p => _serializableProperties.Contains(p.Name))
            .ToArray();
    }
}
