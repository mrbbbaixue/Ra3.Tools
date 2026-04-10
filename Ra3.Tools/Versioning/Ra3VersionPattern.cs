using System.Text.RegularExpressions;

namespace Ra3.Tools.Versioning;

public sealed class Ra3VersionPattern
{
    public string Prefix { get; }

    public string Suffix { get; }

    public string? ChannelSuffix { get; }

    public Ra3VersionPattern(string prefix, string suffix, string? channelSuffix = null)
    {
        Prefix = prefix;
        Suffix = suffix;
        ChannelSuffix = channelSuffix;
    }

    public IReadOnlyList<Ra3Version> MatchMany(IEnumerable<string> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        return inputs.Select(MatchOne).Where(version => version != null).Select(version => version!.Value).ToArray();
    }

    public Ra3Version? MatchOne(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var pattern = BuildPattern();
        var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
        return match.Success && Ra3Version.TryParse(match.Groups["version"].Value, out var version) ? version : null;
    }

    private string BuildPattern()
    {
        var finalCharacter = ChannelSuffix == null ? "[0-9A-Z]" : Regex.Escape(ChannelSuffix);
        return $"{Regex.Escape(Prefix)}(?<version>[1-9]\\.[0-9][0-9]{finalCharacter}){Regex.Escape(Suffix)}";
    }
}
