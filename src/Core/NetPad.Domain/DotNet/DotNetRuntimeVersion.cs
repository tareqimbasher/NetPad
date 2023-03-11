using System;
using System.Reflection;

namespace NetPad.DotNet;

public class DotNetRuntimeVersion
{
    public DotNetRuntimeVersion(string frameworkName, string version)
    {
        FrameworkName = frameworkName;
        Version = version;
    }

    public string FrameworkName { get; }
    public string Version { get; }
    public string Major => Version.Split('.')[0];

    public override string ToString() => $"{FrameworkName} {Version}";

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is not DotNetRuntimeVersion other)
        {
            return false;
        }

        //Same instances must be considered as equal
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        //Must have a IS-A relation of types or must be same type
        var typeOfThis = GetType();
        var typeOfOther = other.GetType();
        if (!typeOfThis.GetTypeInfo().IsAssignableFrom(typeOfOther) && !typeOfOther.GetTypeInfo().IsAssignableFrom(typeOfThis))
        {
            return false;
        }

        return FrameworkName?.Equals(other.FrameworkName) == true && Version?.Equals(other.Version) == true;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return Version.GetHashCode();
    }

    /// <inheritdoc/>
    public static bool operator ==(DotNetRuntimeVersion? left, DotNetRuntimeVersion? right)
    {
        if (Equals(left, null))
        {
            return Equals(right, null);
        }

        if (Equals(right, null))
            return false;

        return left.Equals(right);
    }

    /// <inheritdoc/>
    public static bool operator !=(DotNetRuntimeVersion? left, DotNetRuntimeVersion? right)
    {
        return !(left == right);
    }
}
