using Microsoft.Win32;

namespace Ra3.Tools.Installation;

/// <summary>枚举 RA3 安装支持的语言（Launcher\*.csf 与对应 SkuDef 匹配）并映射显示名。</summary>
public sealed class Ra3LanguageService
{
    private static readonly IReadOnlyDictionary<string, string> DisplayLanguageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["english"] = "English (US)",
        ["chinese_t"] = "Chinese (Traditional)",
        ["chinese_s"] = "Chinese (Simplified)"
    };

    /// <summary>返回安装支持的语言代码列表（同时存在对应 SkuDef 的 csf 文件）。</summary>
    /// <param name="installPath">安装根目录。</param>
    public IReadOnlyList<string> GetValidLanguages(string installPath)
    {
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return Array.Empty<string>();
        }

        var launcherPath = Path.Combine(installPath, "Launcher");
        if (!Directory.Exists(launcherPath))
        {
            return Array.Empty<string>();
        }

        return Directory.EnumerateFiles(launcherPath, "*.csf")
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Where(language => File.Exists(Path.Combine(installPath, $"RA3_{language}_1.12.SkuDef")))
            .ToArray();
    }

    /// <summary>返回缺少对应 SkuDef 的语言文件（孤儿 csf），供修复用。</summary>
    /// <param name="installPath">安装根目录。</param>
    public IReadOnlyList<string> GetInvalidLanguageFiles(string installPath)
    {
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return Array.Empty<string>();
        }

        var launcherPath = Path.Combine(installPath, "Launcher");
        if (!Directory.Exists(launcherPath))
        {
            return Array.Empty<string>();
        }

        return Directory.EnumerateFiles(launcherPath, "*.csf")
            .Where(path =>
            {
                var language = Path.GetFileNameWithoutExtension(path);
                return !File.Exists(Path.Combine(installPath, $"RA3_{language}_1.12.SkuDef"));
            })
            .ToArray();
    }

    internal static string GetDisplayLanguage(string language)
    {
        return DisplayLanguageMap.TryGetValue(language, out var displayLanguage) ? displayLanguage : language;
    }
}
