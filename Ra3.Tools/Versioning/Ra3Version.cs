using System.Text.RegularExpressions;

namespace Ra3.Tools.Versioning;

public readonly record struct Ra3Version(int Major, string MinorPart) : IComparable<Ra3Version>
{
    private static readonly Regex MinorNormalizer = new("[A-Z]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static Ra3Version Parse(string value)
    {
        if (!TryParse(value, out var version))
        {
            throw new FormatException($"Invalid RA3 version: {value}");
        }

        return version;
    }

    public static bool TryParse(string? value, out Ra3Version version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split('.', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !int.TryParse(parts[0], out var major) || parts[1].Length != 3)
        {
            return false;
        }

        if (!parts[1].All(character => char.IsDigit(character) || char.IsAsciiLetterUpper(character)))
        {
            return false;
        }

        version = new Ra3Version(major, parts[1]);
        return true;
    }

    public int CompareTo(Ra3Version other)
    {
        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0)
        {
            return majorComparison;
        }

        return NormalizeMinor(MinorPart).CompareTo(NormalizeMinor(other.MinorPart));
    }

    public static bool operator >(Ra3Version left, Ra3Version right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <(Ra3Version left, Ra3Version right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >=(Ra3Version left, Ra3Version right)
    {
        return left.CompareTo(right) >= 0;
    }

    public static bool operator <=(Ra3Version left, Ra3Version right)
    {
        return left.CompareTo(right) <= 0;
    }

    public override string ToString()
    {
        return $"{Major}.{MinorPart}";
    }

    private static int NormalizeMinor(string value)
    {
        return int.Parse(MinorNormalizer.Replace(value.ToUpperInvariant(), "0"));
    }
}
