using Ra3.Tools.Internal;

namespace Ra3.Tools.Installation;

public static class Ra3KnownPaths
{
    public static string RedAlert3DocumentsDirectoryPath =>
        Path.Combine(SpecialFolderProvider.Current.GetMyDocumentsPath(), "Red Alert 3");

    public static string RedAlert3ApplicationDataDirectoryPath =>
        Path.Combine(SpecialFolderProvider.Current.GetApplicationDataPath(), "Red Alert 3");

    public static string ModsDirectoryPath => Path.Combine(RedAlert3DocumentsDirectoryPath, "Mods");

    public static string ReplaysDirectoryPath => Path.Combine(RedAlert3DocumentsDirectoryPath, "Replays");

    public static string MapsDirectoryPath => Path.Combine(RedAlert3ApplicationDataDirectoryPath, "Maps");

    public static string ProfilesDirectoryPath => Path.Combine(RedAlert3ApplicationDataDirectoryPath, "Profiles");
}
