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
    public void Parse_SupportsTwoDigitMinorVersions()
    {
        Assert.Equal("1.12", Ra3Version.Parse("1.12").ToString());
        Assert.Equal("1.13", Ra3Version.Parse("1.13").ToString());
        Assert.True(Ra3Version.TryParse("1.12", out _));
        Assert.True(Ra3Version.TryParse("1.13", out _));
    }

    [Fact]
    public void Parse_RejectsInvalidMinorVersions()
    {
        Assert.False(Ra3Version.TryParse("1.1", out _));
        Assert.False(Ra3Version.TryParse("1.1234", out _));
        Assert.False(Ra3Version.TryParse("1.A2", out _));
        Assert.False(Ra3Version.TryParse("1.1A2", out _));
        Assert.False(Ra3Version.TryParse("abc", out _));
    }

    [Fact]
    public void Compare_OrdersChannelLetterBeforeReleaseAndDigitsAfter()
    {
        Assert.True(Ra3Version.Parse("1.12A") < Ra3Version.Parse("1.12B"));
        Assert.True(Ra3Version.Parse("1.12B") < Ra3Version.Parse("1.12"));
        Assert.True(Ra3Version.Parse("1.12") < Ra3Version.Parse("1.120"));
        Assert.True(Ra3Version.Parse("1.12A") < Ra3Version.Parse("1.13"));
    }

    [Fact]
    public void Compare_ChannelLetterComparisonIsCaseInsensitive()
    {
        Assert.Equal(0, Ra3Version.Parse("1.12a").CompareTo(Ra3Version.Parse("1.12A")));
    }

    [Fact]
    public void Ra3VersionPattern_MatchesConfiguredPrefixAndSuffix()
    {
        var pattern = new Ra3VersionPattern("mod_", ".pkg");

        var versions = pattern.MatchMany(["mod_3.181.pkg", "mod_4.02N.pkg", "ignore.txt"]);

        Assert.Contains(Ra3Version.Parse("3.181"), versions);
        Assert.Contains(Ra3Version.Parse("4.02N"), versions);
    }

    [Fact]
    public void Ra3VersionPattern_MatchesTwoDigitGameVersion()
    {
        var pattern = new Ra3VersionPattern("ra3_", ".game");

        var version = pattern.MatchOne("ra3_1.12.game");

        Assert.Equal(Ra3Version.Parse("1.12"), version);
    }

    [Fact]
    public void Ra3VersionPattern_ChannelSuffixPinsLastCharacter()
    {
        var pattern = new Ra3VersionPattern("corona_", ".lyi", "N");

        Assert.Equal(Ra3Version.Parse("4.02N"), pattern.MatchOne("corona_4.02N.lyi"));
        Assert.Null(pattern.MatchOne("corona_4.021.lyi"));
    }
}
