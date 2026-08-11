using System.Globalization;

namespace Ra3.Tools.Profiles;

/// <summary>
/// 解析 RA3 档案目录下的 ProfileData.ini，提取玩家公钥（PPK，即游戏 ID）。
/// PPK 格式为纯数字（如 "12345678901234567"）或 "数字:版本"（如 "223456789012345678:1"）。
/// </summary>
public sealed record Ra3ProfileData(long? PublicKey, int? Version)
{
    /// <summary>从文件加载玩家身份；文件不存在返回空记录（PublicKey = Version = null）。</summary>
    /// <param name="path">ProfileData.ini 路径。</param>
    public static Ra3ProfileData Load(string path)
    {
        if (!File.Exists(path))
        {
            return new Ra3ProfileData(null, null);
        }

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            var equalsIndex = line.IndexOf('=');
            if (equalsIndex <= 0 || !line[..equalsIndex].Trim().Equals("PPK", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return Parse(line[(equalsIndex + 1)..].Trim());
        }

        return new Ra3ProfileData(null, null);
    }

    /// <summary>解析 PPK 值；格式无效返回空记录。</summary>
    /// <param name="ppkValue">PPK 值（纯数字或 "数字:版本"）。</param>
    public static Ra3ProfileData Parse(string ppkValue)
    {
        if (string.IsNullOrWhiteSpace(ppkValue))
        {
            return new Ra3ProfileData(null, null);
        }

        var parts = ppkValue.Split(':', 2, StringSplitOptions.TrimEntries);
        if (!long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var publicKey))
        {
            return new Ra3ProfileData(null, null);
        }

        var version = parts.Length == 2 && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedVersion)
            ? (int?)parsedVersion
            : null;
        return new Ra3ProfileData(publicKey, version);
    }
}
