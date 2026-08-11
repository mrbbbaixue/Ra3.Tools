namespace Ra3.Tools.Installation;

/// <summary>
/// 读写 RA3 玩家档案目录（directory.ini），用于枚举档案、获取当前档案和清理 Skirmish.ini。
/// </summary>
public sealed class Ra3ProfileStore
{
    private const string DirectoryIniFileName = "directory.ini";

    // directory.ini 第二行的当前档案前缀长度为 15 字节（实测值，来源为 RA3 生成的 directory.ini）。
    private const int CurrentProfilePrefixLength = 15;

    /// <summary>返回 directory.ini 中登记且实际存在目录的档案名列表。</summary>
    /// <param name="installation">RA3 安装信息。</param>
    public IReadOnlyList<string> GetProfiles(Ra3Installation installation)
    {
        var directoryPath = Path.Combine(installation.ProfilesDirectoryPath, DirectoryIniFileName);
        if (!File.Exists(directoryPath))
        {
            return Array.Empty<string>();
        }

        var lines = File.ReadAllLines(directoryPath);
        if (lines.Length == 0)
        {
            return Array.Empty<string>();
        }

        var original = ParseDirectoryString(lines[0]);
        return Directory.Exists(installation.ProfilesDirectoryPath)
            ? Directory.GetDirectories(installation.ProfilesDirectoryPath)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(profile => profile != null && original.Contains(profile, StringComparison.Ordinal))
                .Cast<string>()
                .ToArray()
            : Array.Empty<string>();
    }

    /// <summary>返回当前档案名；directory.ini 缺失或格式不完整时返回 null。</summary>
    /// <param name="installation">RA3 安装信息。</param>
    public string? GetCurrentProfile(Ra3Installation installation)
    {
        var directoryPath = Path.Combine(installation.ProfilesDirectoryPath, DirectoryIniFileName);
        if (!File.Exists(directoryPath))
        {
            return null;
        }

        var lines = File.ReadAllLines(directoryPath);
        if (lines.Length < 2)
        {
            return null;
        }

        var value = ParseDirectoryString(lines[1]);
        return value.Length > CurrentProfilePrefixLength ? value[CurrentProfilePrefixLength..] : null;
    }

    /// <summary>删除指定档案的 Skirmish.ini（用于清除离线遭遇战残留的房名/密码）。</summary>
    /// <param name="installation">RA3 安装信息。</param>
    /// <param name="profile">档案名。</param>
    public void DeleteSkirmishIni(Ra3Installation installation, string profile)
    {
        var skirmishPath = Path.Combine(installation.ProfilesDirectoryPath, profile, "Skirmish.ini");
        if (File.Exists(skirmishPath))
        {
            File.Delete(skirmishPath);
        }
    }

    /// <summary>删除所有档案的 Skirmish.ini。</summary>
    /// <param name="installation">RA3 安装信息。</param>
    public void DeleteAllSkirmishIni(Ra3Installation installation)
    {
        foreach (var profile in GetProfiles(installation))
        {
            DeleteSkirmishIni(installation, profile);
        }
    }

    // directory.ini 使用 `_XX`（XX 为字节的十六进制）编码非 ASCII/可打印字符。
    private static string ParseDirectoryString(string value)
    {
        var bytes = new List<byte>();
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (current != '_')
            {
                bytes.Add(Convert.ToByte(current));
                continue;
            }

            // 畸形输入（下划线后不足两位十六进制）时放弃解析剩余部分，避免越界崩溃。
            if (index + 2 >= value.Length ||
                !byte.TryParse(value.AsSpan(index + 1, 2), System.Globalization.NumberStyles.HexNumber, null, out var parsed))
            {
                break;
            }

            bytes.Add(parsed);
            index += 2;
        }

        return System.Text.Encoding.Unicode.GetString(bytes.ToArray());
    }
}
