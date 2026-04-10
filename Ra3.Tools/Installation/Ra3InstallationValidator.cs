using Ra3.Tools.Internal;

namespace Ra3.Tools.Installation;

public static class Ra3InstallationValidator
{
    public static bool IsInstallDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return false;
        }

        return File.Exists(Path.Combine(path, "RA3.exe")) &&
               Directory.EnumerateFiles(path, "RA3_*_1.12.SkuDef").Any();
    }

    public static bool HasRequiredGameFiles(string path)
    {
        if (!IsInstallDirectory(path))
        {
            return false;
        }

        return File.Exists(Path.Combine(path, "Data", "ra3_1.12.game"));
    }

    public static bool NeedsSteamAppId(string path)
    {
        if (!path.Contains("steam", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var steamAppIdPath = Path.Combine(path, "steam_appid.txt");
        return !File.Exists(steamAppIdPath) || File.ReadAllText(steamAppIdPath).Trim() != "17480";
    }

    public static bool NeedsPatchedParFile(string path)
    {
        var patchedParPath = Path.Combine(path, "Data", "ra3_1.12.par");
        return File.Exists(patchedParPath) &&
               !File.ReadAllBytes(patchedParPath).SequenceEqual(PatchedParData.Bytes);
    }
}
