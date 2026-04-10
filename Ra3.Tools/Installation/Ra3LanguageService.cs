using Microsoft.Win32;

namespace Ra3.Tools.Installation;

public sealed class Ra3LanguageService
{
    private static readonly IReadOnlyDictionary<string, string> DisplayLanguageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["english"] = "English (US)",
        ["chinese_t"] = "Chinese (Traditional)",
        ["chinese_s"] = "Chinese (Simplified)"
    };

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
