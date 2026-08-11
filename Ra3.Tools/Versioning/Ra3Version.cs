namespace Ra3.Tools.Versioning;

/// <summary>
/// 表示一个 RA3 风格版本号（如 1.12、1.13、4.02N）。
/// 小版本支持 2~3 位，末位可为渠道字母（如 "12"、"120"、"12A"、"02N"）。
/// </summary>
public readonly record struct Ra3Version(int Major, string MinorPart) : IComparable<Ra3Version>
{
    /// <summary>解析版本号字符串，格式无效时抛出 <see cref="FormatException"/>。</summary>
    /// <param name="value">如 "1.12"、"1.120"、"4.02N"。</param>
    public static Ra3Version Parse(string value)
    {
        if (!TryParse(value, out var version))
        {
            throw new FormatException($"Invalid RA3 version: {value}");
        }

        return version;
    }

    /// <summary>
    /// 尝试解析版本号。小版本必须为 2~3 位：除末位外均为数字，末位可为数字或 ASCII 字母
    /// （"1.12"、"1.13" 等游戏版本与 "1.12A"、"4.02N" 等渠道版本均可解析）。
    /// </summary>
    public static bool TryParse(string? value, out Ra3Version version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split('.', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !int.TryParse(parts[0], out var major) || !IsValidMinor(parts[1]))
        {
            return false;
        }

        version = new Ra3Version(major, parts[1]);
        return true;
    }

    /// <summary>
    /// 比较规则：主版本优先；小版本先按数字部分比较，数字相同时带渠道字母的版本
    /// （如 1.12A）小于无字母的正式版本（如 1.12），字母版本之间按字典序比较。
    /// </summary>
    public int CompareTo(Ra3Version other)
    {
        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0)
        {
            return majorComparison;
        }

        return CompareMinor(MinorPart, other.MinorPart);
    }

    /// <summary>判断当前版本是否大于另一个版本。</summary>
    public static bool operator >(Ra3Version left, Ra3Version right)
    {
        return left.CompareTo(right) > 0;
    }

    /// <summary>判断当前版本是否小于另一个版本。</summary>
    public static bool operator <(Ra3Version left, Ra3Version right)
    {
        return left.CompareTo(right) < 0;
    }

    /// <summary>判断当前版本是否大于或等于另一个版本。</summary>
    public static bool operator >=(Ra3Version left, Ra3Version right)
    {
        return left.CompareTo(right) >= 0;
    }

    /// <summary>判断当前版本是否小于或等于另一个版本。</summary>
    public static bool operator <=(Ra3Version left, Ra3Version right)
    {
        return left.CompareTo(right) <= 0;
    }

    /// <summary>以原始字符串形式返回（如 "1.12"、"4.02N"）。</summary>
    public override string ToString()
    {
        return $"{Major}.{MinorPart}";
    }

    private static bool IsValidMinor(string minor)
    {
        if (minor.Length is < 2 or > 3)
        {
            return false;
        }

        for (var index = 0; index < minor.Length; index++)
        {
            var current = minor[index];
            if (char.IsAsciiDigit(current))
            {
                continue;
            }

            if (index == minor.Length - 1 && char.IsAsciiLetter(current))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static int CompareMinor(string left, string right)
    {
        var leftParts = SplitMinor(left);
        var rightParts = SplitMinor(right);
        var numericComparison = leftParts.Numeric.CompareTo(rightParts.Numeric);
        if (numericComparison != 0)
        {
            return numericComparison;
        }

        if (leftParts.Channel is null && rightParts.Channel is not null)
        {
            return 1;
        }

        if (leftParts.Channel is not null && rightParts.Channel is null)
        {
            return -1;
        }

        return leftParts.Channel!.Value.CompareTo(rightParts.Channel!.Value);
    }

    private static (int Numeric, char? Channel) SplitMinor(string minor)
    {
        var last = minor[^1];
        if (!char.IsAsciiLetter(last))
        {
            return (int.Parse(minor), null);
        }

        return (int.Parse(minor[..^1]), char.ToUpperInvariant(last));
    }
}
