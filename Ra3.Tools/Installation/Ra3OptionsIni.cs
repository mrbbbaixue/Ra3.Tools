namespace Ra3.Tools.Installation;

/// <summary>清理 RA3 玩家 Options.ini 中的危险设置（如防火墙端口覆盖）。</summary>
public sealed class Ra3OptionsIni
{
    private const string FirewallPortOverrideKey = "FirewallPortOverride";

    /// <summary>从指定 Options.ini 中移除 FirewallPortOverride 键；有移除返回 true，文件不存在或无需移除返回 false。</summary>
    /// <param name="optionsIniPath">Options.ini 路径。</param>
    public bool RemoveFirewallPortOverride(string optionsIniPath)
    {
        if (!File.Exists(optionsIniPath))
        {
            return false;
        }

        var lines = File.ReadAllLines(optionsIniPath);
        var filtered = lines.Where(line => !IsFirewallPortOverrideLine(line)).ToArray();
        if (filtered.Length == lines.Length)
        {
            return false;
        }

        File.WriteAllLines(optionsIniPath, filtered);
        return true;
    }

    /// <summary>移除所有玩家档案下 Options.ini 的 FirewallPortOverride 键，返回实际修改的文件数。</summary>
    /// <param name="installation">RA3 安装信息。</param>
    public int RemoveFirewallPortOverrideFromAllProfiles(Ra3Installation installation)
    {
        if (!Directory.Exists(installation.ProfilesDirectoryPath))
        {
            return 0;
        }

        var fixedFiles = 0;
        foreach (var profilePath in Directory.GetDirectories(installation.ProfilesDirectoryPath))
        {
            if (RemoveFirewallPortOverride(Path.Combine(profilePath, "Options.ini")))
            {
                fixedFiles++;
            }
        }

        return fixedFiles;
    }

    // 精确匹配 `FirewallPortOverride = ...` 键（大小写不敏感），保留注释行与相似前缀的键。
    private static bool IsFirewallPortOverrideLine(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed[0] is ';' or '#')
        {
            return false;
        }

        var equalsIndex = trimmed.IndexOf('=');
        if (equalsIndex <= 0)
        {
            return false;
        }

        return trimmed[..equalsIndex].Trim().Equals(FirewallPortOverrideKey, StringComparison.OrdinalIgnoreCase);
    }
}
