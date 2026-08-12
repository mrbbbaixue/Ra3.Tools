using Ra3.Tools.Configuration;
using Ra3.Tools.Versioning;

namespace Ra3.Tools.Installation;

/// <summary>
/// 枚举与解析安装目录中的 SkuDef 文件（命名规则 ra3_&lt;语言&gt;_&lt;版本&gt;.skudef，如 RA3_chinese_t_1.12.SkuDef），
/// 与 RA3Bar-RA3Launcher 的 getSkudefs / getItemFromSkudef 语义对齐：按 "-runver" 版本精确匹配或取最高版本。
/// </summary>
public static class Ra3SkudefCatalog
{
    /// <summary>查找安装目录中的版本化 SkuDef 文件（文件名可解析出版本），按版本升序排列。</summary>
    /// <param name="installPath">安装根目录。</param>
    /// <param name="languageName">语言代码（如 "chinese_t"、"english"）；为 null 或空时枚举全部语言。</param>
    public static IReadOnlyList<string> Find(string installPath, string? languageName = null)
    {
        if (string.IsNullOrWhiteSpace(installPath) || !Directory.Exists(installPath))
        {
            return Array.Empty<string>();
        }

        // 与 RA3Bar 的 getSkudefs 一致：文件系统匹配不区分大小写（实际文件是 RA3_chinese_t_1.12.SkuDef）。
        var pattern = string.IsNullOrWhiteSpace(languageName)
            ? "ra3_*_*.skudef"
            : $"ra3_{languageName}_*.skudef";

        return Directory.EnumerateFiles(installPath, pattern)
            .Where(path => TryParseSkudefVersion(path, out _))
            .OrderBy(path => GetSkudefVersion(path))
            .ToArray();
    }

    /// <summary>
    /// 解析启动应使用的 SkuDef 文件完整路径：
    /// 指定 <paramref name="runVersion"/> 时精确匹配该版本（对应 RA3Bar 的 "-runver"），否则返回最高版本。
    /// </summary>
    /// <param name="installPath">安装根目录。</param>
    /// <param name="runVersion">运行版本；为 null 时取最高版本。</param>
    /// <param name="languageName">语言代码；为 null 时枚举全部语言。</param>
    /// <exception cref="FileNotFoundException">目录中没有任何 SkuDef，或找不到指定版本的 SkuDef 时抛出。</exception>
    public static string Resolve(string installPath, Ra3Version? runVersion = null, string? languageName = null)
    {
        var skudefs = Find(installPath, languageName);
        if (skudefs.Count == 0)
        {
            throw new FileNotFoundException($"安装目录中没有找到任何 SkuDef：{installPath}", installPath);
        }

        if (runVersion is { } version)
        {
            var match = skudefs.FirstOrDefault(path => GetSkudefVersion(path) == version);
            if (match is null)
            {
                throw new FileNotFoundException($"没有找到版本 {version} 的 SkuDef：{installPath}", installPath);
            }

            return match;
        }

        return skudefs[^1];
    }

    /// <summary>读取 SkuDef 中 "set-exe" 指定的游戏可执行文件完整路径（相对安装根目录解析，如 "Data\RA3_1.12.game"）。</summary>
    /// <param name="installPath">安装根目录。</param>
    /// <param name="skudefPath">SkuDef 文件完整路径。</param>
    /// <exception cref="FileNotFoundException">SkuDef 文件或 set-exe 指向的可执行文件不存在时抛出。</exception>
    /// <exception cref="InvalidDataException">SkuDef 中缺少 set-exe 条目时抛出。</exception>
    public static string GetGameExecutablePath(string installPath, string skudefPath)
    {
        if (TryGetGameExecutablePath(installPath, skudefPath, out var executablePath))
        {
            return executablePath!;
        }

        throw new InvalidDataException($"SkuDef 中缺少 set-exe 条目：{skudefPath}");
    }

    /// <summary>
    /// 尝试读取 SkuDef 中 "set-exe" 指定的游戏可执行文件完整路径。
    /// set-exe 是游戏数据 SkuDef 独有的条目（如 "Data\RA3_1.12.game"），MOD 的 SkuDef 一般没有这一项；
    /// 没有该条目时返回 false（调用方应回退到游戏默认可执行文件）。
    /// </summary>
    /// <param name="installPath">安装根目录。</param>
    /// <param name="skudefPath">SkuDef 文件完整路径。</param>
    /// <param name="executablePath">set-exe 指定的可执行文件完整路径；无 set-exe 条目时为 null。</param>
    /// <exception cref="FileNotFoundException">SkuDef 文件或 set-exe 指向的可执行文件不存在时抛出。</exception>
    public static bool TryGetGameExecutablePath(string installPath, string skudefPath, out string? executablePath)
    {
        executablePath = null;
        if (!File.Exists(skudefPath))
        {
            throw new FileNotFoundException("SkuDef 文件不存在。", skudefPath);
        }

        var document = SkudefSerializer.Load(skudefPath);
        var setExe = document.Entries.OfType<SetExeEntry>().FirstOrDefault();
        if (setExe is null)
        {
            return false;
        }

        executablePath = Path.GetFullPath(Path.Combine(installPath, setExe.Path));
        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException("set-exe 指定的游戏可执行文件不存在。", executablePath);
        }

        return true;
    }

    /// <summary>读取 MOD 的 SkuDef 中 "mod-game" 头行指定的游戏版本；文件不存在或无该行时返回 null。</summary>
    /// <param name="modSkudefPath">MOD 的 SkuDef 文件完整路径。</param>
    public static Ra3Version? TryGetModGameVersion(string modSkudefPath)
    {
        if (!File.Exists(modSkudefPath))
        {
            return null;
        }

        foreach (var line in File.ReadLines(modSkudefPath))
        {
            if (!line.StartsWith("mod-game ", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return TryParseVersion(line["mod-game ".Length..].Trim(), out var version) ? version : null;
        }

        return null;
    }

    /// <summary>从 SkuDef 文件名解析版本（取最后一个 '_' 之后的部分，如 "RA3_chinese_t_1.12.SkuDef" → 1.12）。</summary>
    internal static bool TryParseSkudefVersion(string skudefPath, out Ra3Version version)
    {
        version = default;
        var fileName = Path.GetFileNameWithoutExtension(skudefPath);
        var lastUnderscore = fileName.LastIndexOf('_');
        if (lastUnderscore < 0 || lastUnderscore == fileName.Length - 1)
        {
            return false;
        }

        return TryParseVersion(fileName[(lastUnderscore + 1)..], out version);
    }

    /// <summary>解析 SkuDef 文件名中的版本号；不可解析时抛异常（仅对 Find 已过滤的文件调用）。</summary>
    internal static Ra3Version GetSkudefVersion(string skudefPath)
    {
        if (!TryParseSkudefVersion(skudefPath, out var version))
        {
            throw new FormatException($"无法从 SkuDef 文件名解析版本：{skudefPath}");
        }

        return version;
    }

    /// <summary>
    /// 宽松版本解析：主版本为整数，小版本为 1~3 位数字（末位可为渠道字母，如 "12A"），
    /// 兼容真实安装中的单数字小版本（"1.0"~"1.9" 会补零为 "00"~"09" 以便与 <see cref="Ra3Version"/> 比较）。
    /// </summary>
    private static bool TryParseVersion(string value, out Ra3Version version)
    {
        version = default;
        var dotIndex = value.IndexOf('.');
        if (dotIndex <= 0 || !int.TryParse(value.AsSpan(0, dotIndex), out _))
        {
            return false;
        }

        var minor = value[(dotIndex + 1)..];
        var digitLength = 0;
        while (digitLength < minor.Length && char.IsAsciiDigit(minor[digitLength]))
        {
            digitLength++;
        }

        if (digitLength == 0)
        {
            return false;
        }

        var digits = minor[..digitLength];
        var channel = minor[digitLength..];
        if (channel.Length > 1 || (channel.Length == 1 && !char.IsAsciiLetter(channel[0])))
        {
            return false;
        }

        if (digits.Length == 1)
        {
            digits = "0" + digits;
        }

        return Ra3Version.TryParse($"{value[..dotIndex]}.{digits}{channel}", out version);
    }
}
