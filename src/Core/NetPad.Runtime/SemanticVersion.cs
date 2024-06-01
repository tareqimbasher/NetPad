// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Original source from https://github.com/OmniSharp/omnisharp-roslyn/blob/2d582b05839dbd23baf6e78fa2279163723a824c/src/OmniSharp.Abstractions/SemanticVersion.cs

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace NetPad;

/// <summary>
/// An implementation of semantic versioning (https://semver.org)
/// </summary>
public sealed class SemanticVersion : IComparable, IComparable<SemanticVersion>, IEquatable<SemanticVersion>
{
    private const string VersionSansRegEx = @"^(?<major>(0|[1-9]\d*))\.(?<minor>(0|[1-9]\d*))\.(?<patch>(0|[1-9]\d*))$";
    private const string LabelRegEx = @"^((?<preLabel>[0-9A-Za-z][0-9A-Za-z\-\.]*))?(\+(?<buildLabel>[0-9A-Za-z][0-9A-Za-z\-\.]*))?$";
    private const string LabelUnitRegEx = @"^[0-9A-Za-z][0-9A-Za-z\-\.]*$";

    private string? _versionString;

    /// <summary>
    /// Construct a SemanticVersion from a string.
    /// </summary>
    /// <param name="version">The version to parse.</param>
    /// <exception cref="FormatException"></exception>
    /// <exception cref="OverflowException"></exception>
    public SemanticVersion(string version)
    {
        var v = Parse(version);

        Major = v.Major;
        Minor = v.Minor;
        Patch = v.Patch < 0 ? 0 : v.Patch;
        PreReleaseLabel = v.PreReleaseLabel;
        BuildLabel = v.BuildLabel;
    }

    /// <summary>
    /// Construct a SemanticVersion from a <see cref="Version"/>.
    /// </summary>
    /// <param name="version">The version to parse.</param>
    /// <exception cref="FormatException"></exception>
    /// <exception cref="OverflowException"></exception>
    public SemanticVersion(Version version)
    {
        Major = version.Major == -1 ? 0 : version.Major;
        Minor = version.Minor == -1 ? 0 : version.Minor;
        Patch = version.Build == -1 ? 0 : version.Build;
    }

    /// <summary>
    /// Construct a SemanticVersion.
    /// </summary>
    /// <param name="major">The major version.</param>
    /// <param name="minor">The minor version.</param>
    /// <param name="patch">The patch version.</param>
    /// <param name="preReleaseLabel">The pre-release label for the version.</param>
    /// <param name="buildLabel">The build metadata for the version.</param>
    /// <exception cref="FormatException">
    /// If <paramref name="preReleaseLabel"/> don't match 'LabelUnitRegEx'.
    /// If <paramref name="buildLabel"/> don't match 'LabelUnitRegEx'.
    /// </exception>
    public SemanticVersion(int major, int minor, int patch, string? preReleaseLabel, string? buildLabel)
        : this(major, minor, patch)
    {
        if (!string.IsNullOrEmpty(preReleaseLabel))
        {
            if (!Regex.IsMatch(preReleaseLabel, LabelUnitRegEx)) throw new FormatException(nameof(preReleaseLabel));

            PreReleaseLabel = preReleaseLabel;
        }

        if (!string.IsNullOrEmpty(buildLabel))
        {
            if (!Regex.IsMatch(buildLabel, LabelUnitRegEx)) throw new FormatException(nameof(buildLabel));

            BuildLabel = buildLabel;
        }
    }

    /// <summary>
    /// Construct a SemanticVersion.
    /// </summary>
    /// <param name="major">The major version.</param>
    /// <param name="minor">The minor version.</param>
    /// <param name="patch">The minor version.</param>
    /// <param name="label">The label for the version.</param>
    /// <exception cref="ArgumentException">
    /// If <paramref name="major"/>, <paramref name="minor"/>, or <paramref name="patch"/> is less than 0.
    /// </exception>
    /// <exception cref="FormatException">
    /// If <paramref name="label"/> don't match 'LabelRegEx'.
    /// </exception>
    public SemanticVersion(int major, int minor, int patch, string label)
        : this(major, minor, patch)
    {
        // We presume the SymVer :
        // 1) major.minor.patch-label
        // 2) 'label' starts with letter or digit.
        if (!string.IsNullOrEmpty(label))
        {
            var match = Regex.Match(label, LabelRegEx);
            if (!match.Success) throw new FormatException(nameof(label));

            PreReleaseLabel = match.Groups["preLabel"].Value;
            BuildLabel = match.Groups["buildLabel"].Value;
        }
    }

    /// <summary>
    /// Construct a SemanticVersion.
    /// </summary>
    /// <param name="major">The major version.</param>
    /// <param name="minor">The minor version.</param>
    /// <param name="patch">The minor version.</param>
    /// <exception cref="ArgumentException">
    /// If <paramref name="major"/>, <paramref name="minor"/>, or <paramref name="patch"/> is less than 0.
    /// </exception>
    public SemanticVersion(int major, int minor, int patch)
    {
        if (major < 0) throw new ArgumentException("Major version cannot be less than 0", nameof(major));
        if (minor < 0) throw new ArgumentException("Minor version cannot be less than 0", nameof(minor));
        if (patch < 0) throw new ArgumentException("Patch version cannot be less than 0", nameof(patch));

        Major = major;
        Minor = minor;
        Patch = patch;
        // We presume:
        // PreReleaseLabel = null;
        // BuildLabel = null;
    }

    /// <summary>
    /// Construct a SemanticVersion.
    /// </summary>
    /// <param name="major">The major version.</param>
    /// <param name="minor">The minor version.</param>
    /// <exception cref="ArgumentException">
    /// If <paramref name="major"/> or <paramref name="minor"/> is less than 0.
    /// </exception>
    public SemanticVersion(int major, int minor) : this(major, minor, 0)
    {
    }

    /// <summary>
    /// Construct a SemanticVersion.
    /// </summary>
    /// <param name="major">The major version.</param>
    /// <exception cref="ArgumentException">
    /// If <paramref name="major"/> is less than 0.
    /// </exception>
    public SemanticVersion(int major) : this(major, 0, 0)
    {
    }

    /// <summary>
    /// The major version number, never negative.
    /// </summary>
    public int Major { get; }

    /// <summary>
    /// The minor version number, never negative.
    /// </summary>
    public int Minor { get; }

    /// <summary>
    /// The patch version, -1 if not specified.
    /// </summary>
    public int Patch { get; }

    /// <summary>
    /// PreReleaseLabel position in the SymVer string 'major.minor.patch-PreReleaseLabel+BuildLabel'.
    /// </summary>
    public string? PreReleaseLabel { get; }

    /// <summary>
    /// BuildLabel position in the SymVer string 'major.minor.patch-PreReleaseLabel+BuildLabel'.
    /// </summary>
    public string? BuildLabel { get; }

    /// <summary>
    /// String representation.
    /// </summary>
    public string String => ToString();

    /// <summary>
    /// Parse <paramref name="version"/> and return the result if it is a valid <see cref="SemanticVersion"/>, otherwise throws an exception.
    /// </summary>
    /// <param name="version">The string to parse.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="FormatException"></exception>
    /// <exception cref="OverflowException"></exception>
    public static SemanticVersion Parse(string version)
    {
        if (version == null) throw new ArgumentNullException(nameof(version));
        if (version == string.Empty) throw new FormatException(nameof(version));

        var r = new VersionResult();
        r.Init(true);
        TryParseVersion(version, ref r);

        return r._parsedVersion;
    }

    /// <summary>
    /// Parse <paramref name="version"/> and return true if it is a valid <see cref="SemanticVersion"/>, otherwise return false.
    /// No exceptions are raised.
    /// </summary>
    /// <param name="version">The string to parse.</param>
    /// <param name="result">The return value when the string is a valid <see cref="SemanticVersion"/></param>
    public static bool TryParse(string? version, [NotNullWhen(true)] out SemanticVersion? result)
    {
        if (version != null)
        {
            var r = new VersionResult();
            r.Init(false);

            if (TryParseVersion(version, ref r))
            {
                result = r._parsedVersion;
                return true;
            }
        }

        result = null;
        return false;
    }

    private static bool TryParseVersion(string version, ref VersionResult result)
    {
        if (version.EndsWith("-") || version.EndsWith("+") || version.EndsWith("."))
        {
            result.SetFailure(ParseFailureKind.FormatException);
            return false;
        }

        string? versionSansLabel = null;
        var major = 0;
        var minor = 0;
        var patch = 0;
        string? preLabel = null;
        string? buildLabel = null;

        // We parse the SymVer 'version' string 'major.minor.patch-PreReleaseLabel+BuildLabel'.
        var dashIndex = version.IndexOf('-');
        var plusIndex = version.IndexOf('+');

        if (dashIndex > plusIndex)
        {
            // 'PreReleaseLabel' can contains dashes.
            if (plusIndex == -1)
            {
                // No buildLabel: buildLabel == null
                // Format is 'major.minor.patch-PreReleaseLabel'
                preLabel = version.Substring(dashIndex + 1);
                versionSansLabel = version.Substring(0, dashIndex);
            }
            else
            {
                // No PreReleaseLabel: preLabel == null
                // Format is 'major.minor.patch+BuildLabel'
                buildLabel = version.Substring(plusIndex + 1);
                versionSansLabel = version.Substring(0, plusIndex);
                dashIndex = -1;
            }
        }
        else
        {
            if (plusIndex == -1)
            {
                // Here dashIndex == plusIndex == -1
                // No preLabel - preLabel == null;
                // No buildLabel - buildLabel == null;
                // Format is 'major.minor.patch'
                versionSansLabel = version;
            }
            else if (dashIndex == -1)
            {
                // No PreReleaseLabel: preLabel == null
                // Format is 'major.minor.patch+BuildLabel'
                buildLabel = version.Substring(plusIndex + 1);
                versionSansLabel = version.Substring(0, plusIndex);
            }
            else
            {
                // Format is 'major.minor.patch-PreReleaseLabel+BuildLabel'
                preLabel = version.Substring(dashIndex + 1, plusIndex - dashIndex - 1);
                buildLabel = version.Substring(plusIndex + 1);
                versionSansLabel = version.Substring(0, dashIndex);
            }
        }

        if ((dashIndex != -1 && string.IsNullOrEmpty(preLabel)) ||
            (plusIndex != -1 && string.IsNullOrEmpty(buildLabel)) ||
            string.IsNullOrEmpty(versionSansLabel))
        {
            // We have dash and no preReleaseLabel  or
            // we have plus and no buildLabel or
            // we have no main version part (versionSansLabel==null)
            result.SetFailure(ParseFailureKind.FormatException);
            return false;
        }

        var match = Regex.Match(versionSansLabel, VersionSansRegEx);
        if (!match.Success)
        {
            result.SetFailure(ParseFailureKind.FormatException);
            return false;
        }

        if (!int.TryParse(match.Groups["major"].Value, out major))
        {
            result.SetFailure(ParseFailureKind.FormatException);
            return false;
        }

        if (match.Groups["minor"].Success && !int.TryParse(match.Groups["minor"].Value, out minor))
        {
            result.SetFailure(ParseFailureKind.FormatException);
            return false;
        }

        if (match.Groups["patch"].Success && !int.TryParse(match.Groups["patch"].Value, out patch))
        {
            result.SetFailure(ParseFailureKind.FormatException);
            return false;
        }

        if (preLabel != null && !Regex.IsMatch(preLabel, LabelUnitRegEx) ||
            (buildLabel != null && !Regex.IsMatch(buildLabel, LabelUnitRegEx)))
        {
            result.SetFailure(ParseFailureKind.FormatException);
            return false;
        }

        result._parsedVersion = new SemanticVersion(major, minor, patch, preLabel, buildLabel);
        return true;
    }

    /// <summary>
    /// Implement ToString()
    /// </summary>
    public override string ToString()
    {
        if (_versionString == null)
        {
            var result = new StringBuilder();

            result.Append(Major).Append('.').Append(Minor).Append('.').Append(Patch);

            if (!string.IsNullOrEmpty(PreReleaseLabel))
            {
                result.Append('-').Append(PreReleaseLabel);
            }

            if (!string.IsNullOrEmpty(BuildLabel))
            {
                result.Append('+').Append(BuildLabel);
            }

            _versionString = result.ToString();
        }

        return _versionString;
    }

    /// <summary>
    /// Implement Compare.
    /// </summary>
    public static int Compare(SemanticVersion? versionA, SemanticVersion? versionB)
    {
        if (versionA != null)
        {
            return versionA.CompareTo(versionB);
        }

        if (versionB != null)
        {
            return -1;
        }

        return 0;
    }

    /// <summary>
    /// Implement <see cref="IComparable.CompareTo"/>
    /// </summary>
    public int CompareTo(object? version)
    {
        if (version == null)
        {
            return 1;
        }

        return CompareTo(version as SemanticVersion);
    }

    /// <summary>
    /// Implement <see cref="IComparable{T}.CompareTo"/>.
    /// Meets SymVer 2.0 p.11 https://semver.org/
    /// </summary>
    public int CompareTo(SemanticVersion? value)
    {
        if (value is null)
            return 1;

        if (Major != value.Major)
            return Major > value.Major ? 1 : -1;

        if (Minor != value.Minor)
            return Minor > value.Minor ? 1 : -1;

        if (Patch != value.Patch)
            return Patch > value.Patch ? 1 : -1;

        // SymVer 2.0 standard requires to ignore 'BuildLabel' (Build metadata).
        return ComparePreLabel(this.PreReleaseLabel, value.PreReleaseLabel);
    }

    /// <summary>
    /// Override <see cref="object.Equals(object)"/>
    /// </summary>
    public override bool Equals(object? obj)
    {
        return Equals(obj as SemanticVersion);
    }

    /// <summary>
    /// Implement <see cref="IEquatable{T}.Equals(T)"/>
    /// </summary>
    public bool Equals(SemanticVersion? other)
    {
        // SymVer 2.0 standard requires to ignore 'BuildLabel' (Build metadata).
        return other != null &&
               (Major == other.Major) && (Minor == other.Minor) && (Patch == other.Patch) &&
               string.Equals(PreReleaseLabel, other.PreReleaseLabel, StringComparison.Ordinal);
    }

    /// <summary>
    /// Override <see cref="object.GetHashCode()"/>
    /// </summary>
    public override int GetHashCode()
    {
        return this.ToString().GetHashCode();
    }

    /// <summary>
    /// Overloaded == operator.
    /// </summary>
    public static bool operator ==(SemanticVersion? v1, SemanticVersion? v2)
    {
        if (v1 is null)
        {
            return v2 is null;
        }

        return v1.Equals(v2);
    }

    /// <summary>
    /// Overloaded != operator.
    /// </summary>
    public static bool operator !=(SemanticVersion? v1, SemanticVersion? v2)
    {
        return !(v1 == v2);
    }

    /// <summary>
    /// Overloaded &lt; operator.
    /// </summary>
    public static bool operator <(SemanticVersion v1, SemanticVersion v2)
    {
        return (Compare(v1, v2) < 0);
    }

    /// <summary>
    /// Overloaded &lt;= operator.
    /// </summary>
    public static bool operator <=(SemanticVersion v1, SemanticVersion v2)
    {
        return (Compare(v1, v2) <= 0);
    }

    /// <summary>
    /// Overloaded &gt; operator.
    /// </summary>
    public static bool operator >(SemanticVersion v1, SemanticVersion v2)
    {
        return (Compare(v1, v2) > 0);
    }

    /// <summary>
    /// Overloaded &gt;= operator.
    /// </summary>
    public static bool operator >=(SemanticVersion v1, SemanticVersion v2)
    {
        return (Compare(v1, v2) >= 0);
    }

    private static int ComparePreLabel(string? preLabel1, string? preLabel2)
    {
        // Symver 2.0 standard p.9
        // Pre-release versions have a lower precedence than the associated normal version.
        // Comparing each dot separated identifier from left to right
        // until a difference is found as follows:
        //     identifiers consisting of only digits are compared numerically
        //     and identifiers with letters or hyphens are compared lexically in ASCII sort order.
        // Numeric identifiers always have lower precedence than non-numeric identifiers.
        // A larger set of pre-release fields has a higher precedence than a smaller set,
        // if all of the preceding identifiers are equal.
        if (string.IsNullOrEmpty(preLabel1))
        {
            return string.IsNullOrEmpty(preLabel2) ? 0 : 1;
        }

        if (string.IsNullOrEmpty(preLabel2))
        {
            return -1;
        }

        var units1 = preLabel1.Split('.');
        var units2 = preLabel2.Split('.');

        var minLength = units1.Length < units2.Length ? units1.Length : units2.Length;

        for (int i = 0; i < minLength; i++)
        {
            var ac = units1[i];
            var bc = units2[i];
            int number1, number2;
            var isNumber1 = Int32.TryParse(ac, out number1);
            var isNumber2 = Int32.TryParse(bc, out number2);

            if (isNumber1 && isNumber2)
            {
                if (number1 != number2)
                {
                    return number1 < number2 ? -1 : 1;
                }
            }
            else
            {
                if (isNumber1)
                {
                    return -1;
                }

                if (isNumber2)
                {
                    return 1;
                }

                int result = string.CompareOrdinal(ac, bc);
                if (result != 0)
                {
                    return result;
                }
            }
        }

        return units1.Length.CompareTo(units2.Length);
    }

    internal enum ParseFailureKind
    {
        ArgumentException,
        ArgumentOutOfRangeException,
        FormatException
    }

    internal struct VersionResult
    {
        internal SemanticVersion _parsedVersion;
        internal ParseFailureKind _failure;
        internal string _exceptionArgument;
        internal bool _canThrow;

        internal void Init(bool canThrow)
        {
            _canThrow = canThrow;
        }

        internal void SetFailure(ParseFailureKind failure)
        {
            SetFailure(failure, string.Empty);
        }

        internal void SetFailure(ParseFailureKind failure, string argument)
        {
            _failure = failure;
            _exceptionArgument = argument;
            if (_canThrow)
            {
                throw GetVersionParseException();
            }
        }

        internal Exception GetVersionParseException()
        {
            switch (_failure)
            {
                case ParseFailureKind.ArgumentException:
                    return new ArgumentException("version");
                case ParseFailureKind.ArgumentOutOfRangeException:
                    throw new ArgumentOutOfRangeException("version");
                case ParseFailureKind.FormatException:
                    // Regenerate the FormatException as would be thrown by Int32.Parse()
                    try
                    {
                        int.Parse(_exceptionArgument, CultureInfo.InvariantCulture);
                    }
                    catch (FormatException e)
                    {
                        return e;
                    }
                    catch (OverflowException e)
                    {
                        return e;
                    }

                    break;
            }

            return new ArgumentException("version");
        }
    }
}
