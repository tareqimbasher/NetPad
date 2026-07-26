using NetPad.Packages;
using NetPad.Packages.NuGet;

namespace NetPad.Runtime.Tests.Packages.NuGet;

public class PackageSearchMergeTests
{
    private static PackageMetadata Package(string packageId, string? version, string? latest = null)
    {
        return new PackageMetadata(packageId, packageId)
        {
            Version = version,
            LatestAvailableVersion = latest
        };
    }

    [Fact]
    public void Keeps_Source_Order_And_All_Distinct_Packages()
    {
        var merged = PackageSearchMerge.ByPackageId([
            [Package("A", "1.0.0"), Package("B", "1.0.0")],
            [Package("C", "1.0.0")]
        ]);

        Assert.Equal(["A", "B", "C"], merged.Select(p => p.PackageId));
    }

    [Fact]
    public void Collapses_The_Same_Package_Id_From_Several_Sources()
    {
        var merged = PackageSearchMerge.ByPackageId([
            [Package("A", "1.0.0")],
            [Package("A", "1.0.0")]
        ]);

        Assert.Single(merged);
    }

    [Fact]
    public void Matches_Package_Ids_Ignoring_Case()
    {
        var merged = PackageSearchMerge.ByPackageId([
            [Package("Newtonsoft.Json", "13.0.1")],
            [Package("NEWTONSOFT.JSON", "13.0.3")]
        ]);

        var package = Assert.Single(merged);
        Assert.Equal("13.0.3", package.Version);
    }

    [Fact]
    public void Newer_Latest_Version_Wins_Regardless_Of_Source_Order()
    {
        var firstSourceWins = PackageSearchMerge.ByPackageId([
            [Package("A", "2.0.0")],
            [Package("A", "1.0.0")]
        ]);

        var secondSourceWins = PackageSearchMerge.ByPackageId([
            [Package("A", "1.0.0")],
            [Package("A", "2.0.0")]
        ]);

        Assert.Equal("2.0.0", Assert.Single(firstSourceWins).Version);
        Assert.Equal("2.0.0", Assert.Single(secondSourceWins).Version);
    }

    [Fact]
    public void Compares_On_Latest_Available_Version_When_It_Is_Known()
    {
        var merged = PackageSearchMerge.ByPackageId([
            [Package("A", "1.0.0", latest: "3.0.0")],
            [Package("A", "2.0.0", latest: "2.0.0")]
        ]);

        Assert.Equal("1.0.0", Assert.Single(merged).Version);
    }

    [Fact]
    public void Winner_Of_Equal_Versions_Is_The_Entry_From_The_First_Source()
    {
        var first = Package("A", "1.0.0");
        var second = Package("A", "1.0.0");

        var merged = PackageSearchMerge.ByPackageId([[first], [second]]);

        Assert.Same(first, Assert.Single(merged));
    }

    [Fact]
    public void An_Entry_With_A_Version_Beats_One_Without()
    {
        var withVersion = Package("A", "1.0.0");
        var withoutVersion = Package("A", null);

        var versionSecond = PackageSearchMerge.ByPackageId([[withoutVersion], [withVersion]]);
        var versionFirst = PackageSearchMerge.ByPackageId([[withVersion], [withoutVersion]]);

        Assert.Same(withVersion, Assert.Single(versionSecond));
        Assert.Same(withVersion, Assert.Single(versionFirst));
    }

    [Fact]
    public void An_Unparseable_Version_Counts_As_No_Version()
    {
        var unparseable = Package("A", "not-a-version");
        var parseable = Package("A", "1.0.0");

        var merged = PackageSearchMerge.ByPackageId([[unparseable], [parseable]]);

        Assert.Same(parseable, Assert.Single(merged));
    }

    [Fact]
    public void Two_Entries_Without_A_Usable_Version_Keep_Source_Order()
    {
        var first = Package("A", null);
        var second = Package("A", "not-a-version");

        var merged = PackageSearchMerge.ByPackageId([[first], [second]]);

        Assert.Same(first, Assert.Single(merged));
    }

    [Fact]
    public void Pre_Release_Loses_To_The_Stable_Release_Of_The_Same_Version()
    {
        var stable = Package("A", "2.0.0");
        var preRelease = Package("A", "2.0.0-beta1");

        var merged = PackageSearchMerge.ByPackageId([[preRelease], [stable]]);

        Assert.Same(stable, Assert.Single(merged));
    }

    [Fact]
    public void Handles_No_Sources_And_Empty_Sources()
    {
        Assert.Empty(PackageSearchMerge.ByPackageId([]));
        Assert.Empty(PackageSearchMerge.ByPackageId([[], []]));
    }

    [Fact]
    public void CompareLatestVersion_Orders_By_The_Newest_Known_Version()
    {
        Assert.True(PackageSearchMerge.CompareLatestVersion(Package("A", "2.0.0"), Package("A", "1.0.0")) > 0);
        Assert.True(PackageSearchMerge.CompareLatestVersion(Package("A", "1.0.0"), Package("A", "2.0.0")) < 0);
        Assert.Equal(0, PackageSearchMerge.CompareLatestVersion(Package("A", "1.0.0"), Package("A", "1.0.0")));
        Assert.True(PackageSearchMerge.CompareLatestVersion(Package("A", "1.0.0"), Package("A", null)) > 0);
        Assert.True(PackageSearchMerge.CompareLatestVersion(Package("A", null), Package("A", "1.0.0")) < 0);
        Assert.Equal(0, PackageSearchMerge.CompareLatestVersion(Package("A", null), Package("A", null)));
    }
}
