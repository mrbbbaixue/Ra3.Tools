using System.Text.RegularExpressions;

namespace Ra3.Tools.Versioning;

/// <summary>
/// 在带前后缀的字符串中匹配 RA3 版本号（如从 "mod_3.181.pkg" 中提取 3.181）。
/// 默认匹配 2~3 位小版本；指定 <see cref="ChannelSuffix"/> 时匹配固定渠道字母结尾的三位版本。
/// </summary>
public sealed class Ra3VersionPattern
{
    /// <summary>版本号之前的固定前缀。</summary>
    public string Prefix { get; }

    /// <summary>版本号之后的固定后缀。</summary>
    public string Suffix { get; }

    /// <summary>可选的固定渠道字母（如 "A"），非空时仅匹配该字母结尾的版本。</summary>
    public string? ChannelSuffix { get; }

    /// <param name="prefix">版本号之前的固定前缀。</param>
    /// <param name="suffix">版本号之后的固定后缀。</param>
    /// <param name="channelSuffix">可选的固定渠道字母。</param>
    public Ra3VersionPattern(string prefix, string suffix, string? channelSuffix = null)
    {
        Prefix = prefix;
        Suffix = suffix;
        ChannelSuffix = channelSuffix;
    }

    /// <summary>从多个输入字符串中提取所有匹配的版本。</summary>
    /// <param name="inputs">候选字符串集合。</param>
    public IReadOnlyList<Ra3Version> MatchMany(IEnumerable<string> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        return inputs.Select(MatchOne).Where(version => version != null).Select(version => version!.Value).ToArray();
    }

    /// <summary>从单个输入字符串中提取版本；不匹配时返回 null。</summary>
    /// <param name="input">候选字符串。</param>
    public Ra3Version? MatchOne(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var pattern = BuildPattern();
        var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
        return match.Success && Ra3Version.TryParse(match.Groups["version"].Value, out var version) ? version : null;
    }

    private string BuildPattern()
    {
        var minorPattern = ChannelSuffix == null ? "[0-9][0-9][0-9A-Z]?" : $"[0-9][0-9]{Regex.Escape(ChannelSuffix)}";
        return $"{Regex.Escape(Prefix)}(?<version>[1-9]\\.{minorPattern}){Regex.Escape(Suffix)}";
    }
}
