using Ra3.Tools.Internal;

namespace Ra3.Tools.Installation;

/// <summary>RA3 常用路径（我的文档 / %appdata% 下的 Red Alert 3 相关目录）。</summary>
public static class Ra3KnownPaths
{
    /// <summary>我的文档\Red Alert 3。</summary>
    public static string RedAlert3DocumentsDirectoryPath =>
        Path.Combine(SpecialFolderProvider.Current.GetMyDocumentsPath(), "Red Alert 3");

    /// <summary>%appdata%\Red Alert 3。</summary>
    public static string RedAlert3ApplicationDataDirectoryPath =>
        Path.Combine(SpecialFolderProvider.Current.GetApplicationDataPath(), "Red Alert 3");

    /// <summary>MOD 目录。</summary>
    public static string ModsDirectoryPath => Path.Combine(RedAlert3DocumentsDirectoryPath, "Mods");

    /// <summary>录像目录。</summary>
    public static string ReplaysDirectoryPath => Path.Combine(RedAlert3DocumentsDirectoryPath, "Replays");

    /// <summary>地图目录。</summary>
    public static string MapsDirectoryPath => Path.Combine(RedAlert3ApplicationDataDirectoryPath, "Maps");

    /// <summary>玩家档案目录。</summary>
    public static string ProfilesDirectoryPath => Path.Combine(RedAlert3ApplicationDataDirectoryPath, "Profiles");
}
