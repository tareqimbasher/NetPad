using Xunit;

namespace NetPad.Runtime.Tests;

public class SemanticVersionTests
{
    public static IEnumerable<object?[]> GetStringVersionParseTestData()
    {
        return new object?[][]
        {
            ["1.0.0", 1, 0, 0, null, null],
            ["0.1.0", 0, 1, 0, null, null],
            ["0.0.1", 0, 0, 1, null, null],
            ["1.0.3-alpha", 1, 0, 3, "alpha", null],
            ["1.0.0-alpha.023", 1, 0, 0, "alpha.023", null],
            ["0.2.0+META", 0, 2, 0, null, "META"],
            ["1.1.0-alpha+META", 1, 1, 0, "alpha", "META"],
            ["1.2.3-alpha.023+META.024", 1, 2, 3, "alpha.023", "META.024"],
        };
    }

    [Theory]
    [MemberData(nameof(GetStringVersionParseTestData))]
    public void ValidVersionStringsParseUsingConstructor(
        string versionStr,
        int expectedMajor,
        int expectedMinor,
        int expectedPatch,
        string? expectedPrereleaseLabel,
        string? expectedBuildLabel)
    {
        var version = new SemanticVersion(versionStr);

        Assert.Equal(versionStr, version.ToString());
        Assert.Equal(expectedMajor, version.Major);
        Assert.Equal(expectedMinor, version.Minor);
        Assert.Equal(expectedPatch, version.Patch);
        Assert.Equal(expectedPrereleaseLabel, version.PreReleaseLabel);
        Assert.Equal(expectedBuildLabel, version.BuildLabel);
    }

    [Theory]
    [MemberData(nameof(GetStringVersionParseTestData))]
    public void ValidSemanticVersionsParse(
        string version,
        int expectedMajor,
        int expectedMinor,
        int expectedPatch,
        string? expectedPrereleaseLabel,
        string? expectedBuildLabel)
    {
        var parsed = SemanticVersion.TryParse(version, out var result);

        Assert.True(parsed, $"{version} did not successfully parse.");
        Assert.Equal(version, result!.ToString());
        Assert.Equal(expectedMajor, result.Major);
        Assert.Equal(expectedMinor, result.Minor);
        Assert.Equal(expectedPatch, result.Patch);
        Assert.Equal(expectedPrereleaseLabel, result.PreReleaseLabel);
        Assert.Equal(expectedBuildLabel, result.BuildLabel);
    }

    [Theory]
    [InlineData("1.2.3", false)]
    [InlineData("1.2.3+0", false)]
    [InlineData("1.2.3+321", false)]
    [InlineData("1.2.3+XYZ", false)]
    [InlineData("1.2.3-alpha", true)]
    [InlineData("1.2.3-alpha+0", true)]
    [InlineData("1.2.3-alpha+321", true)]
    [InlineData("1.2.3-alpha+XYZ", true)]
    public void PrereleaseSemanticVersionsAreIdentified(string version, bool expectedIsPrerelease)
    {
        var semanticVersion = SemanticVersion.Parse(version);

        Assert.Equal(expectedIsPrerelease, semanticVersion.IsPrerelease);
    }

    public static IEnumerable<object?[]> GetDotNetVersionTestData()
    {
        return new[]
        {
            [new Version(1, 0, 0), 1, 0, 0, null, null],
            [new Version(0, 1, 0), 0, 1, 0, null, null],
            [new Version(0, 0, 1), 0, 0, 1, null, null],
            new object?[] { new Version(1, 2, 3), 1, 2, 3, null, null },
        };
    }

    [Theory]
    [MemberData(nameof(GetDotNetVersionTestData))]
    public void DotNetVersionsCanBeParsed(
        Version version,
        int expectedMajor,
        int expectedMinor,
        int expectedPatch,
        string? expectedPrereleaseLabel,
        string? expectedBuildLabel)
    {
        var parsed = new SemanticVersion(version);

        Assert.Equal($"{version.Major}.{version.Minor}.{version.Build}", parsed.ToString());
        Assert.Equal(expectedMajor, parsed.Major);
        Assert.Equal(expectedMinor, parsed.Minor);
        Assert.Equal(expectedPatch, parsed.Patch);
        Assert.Equal(expectedPrereleaseLabel, parsed.PreReleaseLabel);
        Assert.Equal(expectedBuildLabel, parsed.BuildLabel);
    }

    [Theory]
    [InlineData("2.7")]
    [InlineData("1.3.4.5")]
    [InlineData("1.3-alpha")]
    [InlineData("1.3 .4")]
    [InlineData("2.3.18.2-a")]
    [InlineData("01.2.3")]
    [InlineData("1.02.3")]
    [InlineData("1.2.03")]
    [InlineData(".2.03")]
    [InlineData("1.2.")]
    [InlineData("1.2.3-a$b")]
    [InlineData("a.b.c")]
    public void InvalidSemanticVersionsDoNotParse(string version)
    {
        var result = SemanticVersion.TryParse(version, out var semanticVersion);

        Assert.False(result, $"{version} successfully parsed.");
        Assert.Null(semanticVersion);
    }

    [Theory]
    [InlineData("1.2.3", "1.2.3+0")]
    [InlineData("1.2.3+0", "1.2.3+321")]
    [InlineData("1.2.3+321", "1.2.3+XYZ")]
    [InlineData("1.2.3+XYZ", "1.2.3")]
    [InlineData("1.2.3-alpha", "1.2.3-alpha+0")]
    [InlineData("1.2.3-alpha+0", "1.2.3-alpha+321")]
    [InlineData("1.2.3-alpha+321", "1.2.3-alpha+XYZ")]
    [InlineData("1.2.3-alpha+XYZ", "1.2.3-alpha")]
    public void SemanticVersionsAreEqual(string version1, string version2)
    {
        Assert.True(SemanticVersion.TryParse(version1, out var result1));
        Assert.True(SemanticVersion.TryParse(version2, out var result2));

        Assert.True(result1.Equals(result2));
        Assert.True(result2.Equals(result1));

        Assert.Equal(0, result1.CompareTo(result2));
        Assert.Equal(0, result2.CompareTo(result1));
    }

    [Theory]
    [InlineData("1.2.3-alpha", "1.2.3-beta")]
    [InlineData("1.2.3-alpha", "1.2.3")]
    [InlineData("1.2.3", "1.2.4")]
    [InlineData("1.2.3", "1.2.4-alpha")]
    [InlineData("1.2.3", "1.3.0")]
    [InlineData("1.2.3", "1.3.0-alpha")]
    [InlineData("1.2.3", "2.0.0")]
    [InlineData("1.2.3", "2.0.0-alpha")]
    public void SemanticVersionsCanBeCompared(string lowerVersion, string higherVersion)
    {
        Assert.True(SemanticVersion.TryParse(lowerVersion, out var lowerResult));
        Assert.True(SemanticVersion.TryParse(higherVersion, out var higherResult));

        Assert.Equal(-1, lowerResult.CompareTo(higherResult));
        Assert.Equal(1, higherResult.CompareTo(lowerResult));

        Assert.True(higherResult > lowerResult);
        Assert.True(lowerResult < higherResult);
    }
}
