using Ra3.Tools.Versioning;

namespace Ra3.Tools.Test.Versioning;

public sealed class Ra3VersionTests
{
    [Fact]
    public void ParseAndCompare_HandleThreeDigitAndLetterVersions()
    {
        Assert.True(Ra3Version.Parse("4.02N") > Ra3Version.Parse("4.001"));
        Assert.True(Ra3Version.Parse("3.181") > Ra3Version.Parse("3.18A"));
    }

    [Fact]
    public void Ra3VersionPattern_MatchesConfiguredPrefixAndSuffix()
    {
        var pattern = new Ra3VersionPattern("mod_", ".pkg");

        var versions = pattern.MatchMany(["mod_3.181.pkg", "mod_4.02N.pkg", "ignore.txt"]);

        Assert.Contains(Ra3Version.Parse("3.181"), versions);
        Assert.Contains(Ra3Version.Parse("4.02N"), versions);
    }
}
