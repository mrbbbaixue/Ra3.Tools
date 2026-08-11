namespace Ra3.Tools.Installation;

/// <summary>表示一个 RA3 安装目录，提供游戏与常用子目录的解析路径。</summary>
public sealed record Ra3Installation
{
    private readonly string _gameExecutablePath;
    private readonly string _gameDataExecutablePath;

    /// <summary>RA3 安装根目录（自动规范化）。</summary>
    public string RootPath { get; }

    /// <param name="rootPath">RA3 安装根目录。</param>
    public Ra3Installation(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        RootPath = Path.GetFullPath(rootPath);
        _gameExecutablePath = Path.Combine(RootPath, "RA3.exe");
        _gameDataExecutablePath = Path.Combine(RootPath, "Data", "ra3_1.12.game");
    }

    /// <summary>RA3.exe 的完整路径。</summary>
    public string GameExecutablePath => _gameExecutablePath;

    /// <summary>游戏数据可执行文件 Data\ra3_1.12.game 的完整路径。</summary>
    public string GameDataExecutablePath => _gameDataExecutablePath;

    /// <summary>MOD 目录（我的文档\Red Alert 3\Mods）。</summary>
    public string ModsDirectoryPath => Ra3KnownPaths.ModsDirectoryPath;

    /// <summary>录像目录（我的文档\Red Alert 3\Replays）。</summary>
    public string ReplaysDirectoryPath => Ra3KnownPaths.ReplaysDirectoryPath;

    /// <summary>地图目录（%appdata%\Red Alert 3\Maps）。</summary>
    public string MapsDirectoryPath => Ra3KnownPaths.MapsDirectoryPath;

    /// <summary>玩家档案目录（%appdata%\Red Alert 3\Profiles）。</summary>
    public string ProfilesDirectoryPath => Ra3KnownPaths.ProfilesDirectoryPath;
}
