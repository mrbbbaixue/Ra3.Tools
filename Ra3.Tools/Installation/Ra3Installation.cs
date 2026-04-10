namespace Ra3.Tools.Installation;

public sealed record Ra3Installation
{
    public string RootPath { get; }

    public Ra3Installation(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        RootPath = Path.GetFullPath(rootPath);
    }

    public string GameExecutablePath => Path.Combine(RootPath, "RA3.exe");

    public string GameDataExecutablePath => Path.Combine(RootPath, "Data", "ra3_1.12.game");

    public string ModsDirectoryPath => Ra3KnownPaths.ModsDirectoryPath;

    public string ReplaysDirectoryPath => Ra3KnownPaths.ReplaysDirectoryPath;

    public string MapsDirectoryPath => Ra3KnownPaths.MapsDirectoryPath;

    public string ProfilesDirectoryPath => Ra3KnownPaths.ProfilesDirectoryPath;
}
