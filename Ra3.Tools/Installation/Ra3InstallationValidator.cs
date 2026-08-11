using Ra3.Tools.Internal;

namespace Ra3.Tools.Installation;

/// <summary>RA3 安装目录有效性检查与常见修复（Steam AppID、加密 PAR 文件）。</summary>
public static class Ra3InstallationValidator
{
    private const string SteamAppIdValue = "17480";

    /// <summary>判断目录是否为有效的 RA3 安装目录（存在 RA3.exe 且含 "RA3_*_1.12.SkuDef"）。</summary>
    /// <param name="path">候选安装根目录。</param>
    public static bool IsInstallDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return false;
        }

        return File.Exists(Path.Combine(path, "RA3.exe")) &&
               Directory.EnumerateFiles(path, "RA3_*_1.12.SkuDef").Any();
    }

    /// <summary>判断安装目录是否包含游戏必需文件（Data\ra3_1.12.game）。</summary>
    /// <param name="path">安装根目录。</param>
    public static bool HasRequiredGameFiles(string path)
    {
        if (!IsInstallDirectory(path))
        {
            return false;
        }

        return File.Exists(Path.Combine(path, "Data", "ra3_1.12.game"));
    }

    /// <summary>判断 Steam 版安装目录是否需要写入 steam_appid.txt（路径含 "steam" 且文件缺失或内容不是 "17480" 时返回 true）。</summary>
    /// <param name="path">安装根目录。</param>
    public static bool NeedsSteamAppId(string path)
    {
        if (!path.Contains("steam", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var steamAppIdPath = Path.Combine(path, "steam_appid.txt");
        return !File.Exists(steamAppIdPath) || File.ReadAllText(steamAppIdPath).Trim() != SteamAppIdValue;
    }

    /// <summary>为 Steam 版安装目录生成 steam_appid.txt（内容 "17480"）；非 Steam 路径也直接写入，幂等。</summary>
    /// <param name="path">安装根目录。</param>
    /// <exception cref="DirectoryNotFoundException">目录不存在时抛出。</exception>
    public static void GenerateSteamAppId(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Install directory not found: {path}");
        }

        File.WriteAllText(Path.Combine(path, "steam_appid.txt"), SteamAppIdValue);
    }

    /// <summary>判断 Data\ra3_1.12.par 是否已打补丁（存在但与内置补丁数据不一致时返回 true）。</summary>
    /// <param name="path">安装根目录。</param>
    public static bool NeedsPatchedParFile(string path)
    {
        var patchedParPath = Path.Combine(path, "Data", "ra3_1.12.par");
        return File.Exists(patchedParPath) &&
               !File.ReadAllBytes(patchedParPath).SequenceEqual(PatchedParData.Bytes);
    }
}
