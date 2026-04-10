using Microsoft.Win32;
using Ra3.Tools.Interop.Windows;

namespace Ra3.Tools.Installation;

public sealed class Ra3Registry
{
    private const string GameRegistryPath = @"Software\Electronic Arts\Electronic Arts\Red Alert 3";
    private const string ErgcRegistryPath = @"Software\Electronic Arts\Electronic Arts\Red Alert 3\ergc";

    private readonly IRegistryBackend _backend;

    public Ra3Registry() : this(new WindowsRegistryBackend())
    {
    }

    internal Ra3Registry(IRegistryBackend backend)
    {
        _backend = backend;
    }

    public string GetInstallPath()
    {
        return _backend.GetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "Install Dir") as string ?? string.Empty;
    }

    public void SetInstallPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "Install Dir", fullPath, RegistryValueKind.String);
    }

    public RegistryStatus GetStatus()
    {
        if (!_backend.SubKeyExists(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath))
        {
            return RegistryStatus.MissingGameKey;
        }

        var installPath = GetInstallPath();
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return RegistryStatus.MissingInstallPath;
        }

        if (!Ra3InstallationValidator.IsInstallDirectory(installPath))
        {
            return RegistryStatus.InvalidInstallPath;
        }

        if (string.IsNullOrWhiteSpace(_backend.GetValue(RegistryHive.LocalMachine, RegistryView.Registry32, ErgcRegistryPath, null) as string))
        {
            return RegistryStatus.MissingProductKey;
        }

        var mapSync = _backend.GetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "UseLocalUserMaps") as int?
                      ?? _backend.GetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "UseLocalUserMap") as int?
                      ?? -1;
        if (mapSync != 0)
        {
            return RegistryStatus.MissingMapSync;
        }

        var language = _backend.GetValue(RegistryHive.CurrentUser, RegistryView.Default, GameRegistryPath, "Language") as string;
        if (string.IsNullOrWhiteSpace(language))
        {
            return RegistryStatus.MissingCurrentUserLanguage;
        }

        return RegistryStatus.Correct;
    }

    public void EnableMapSync()
    {
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "UseLocalUserMaps", 0, RegistryValueKind.DWord);
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "UseLocalUserMap", 0, RegistryValueKind.DWord);
    }

    public void Clear()
    {
        _backend.DeleteSubKeyTree(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath);
        _backend.DeleteSubKeyTree(RegistryHive.CurrentUser, RegistryView.Default, GameRegistryPath);
    }

    public void Rebuild(string installPath, string key)
    {
        var fullInstallPath = Path.GetFullPath(installPath);
        var readmePath = Path.GetFullPath(Path.Combine(fullInstallPath, "Support", "readme.txt"));

        Clear();

        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "CD Drive", fullInstallPath[..1], RegistryValueKind.String);
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "DisplayName", "Command & Conquer Red Alert 3", RegistryValueKind.String);
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "Install Dir", fullInstallPath, RegistryValueKind.String);
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "Installed From", fullInstallPath[..1], RegistryValueKind.String);
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "language", "English (US)", RegistryValueKind.String);
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "lastversion", string.Empty, RegistryValueKind.String);
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "Patch URL", "http://www.ea.com/redalert", RegistryValueKind.String);
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "Product GUID", "{296D8550-CB06-48E4-9A8B-E5034FB64715}", RegistryValueKind.String);
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "Product Name", "Command & Conquer Red Alert 3", RegistryValueKind.String);
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "ProfileFolderName", "Profiles", RegistryValueKind.String);
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "Readme", readmePath, RegistryValueKind.String);
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "Registration", @"Software\Electronic Arts\Electronic Arts\Red Alert 3\ergc", RegistryValueKind.String);
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "ReplayFolderName", "Replays", RegistryValueKind.String);
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "SaveFolderName", "SaveGames", RegistryValueKind.String);
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "ScreenshotsFolderName", "Screenshots", RegistryValueKind.String);
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "Suppression Exe", string.Empty, RegistryValueKind.String);
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "UserDataLeafName", "Red Alert 3", RegistryValueKind.String);
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, ErgcRegistryPath, null, key, RegistryValueKind.String);

        EnableMapSync();

        _backend.SetValue(RegistryHive.CurrentUser, RegistryView.Default, GameRegistryPath, "Language", "english", RegistryValueKind.String);
    }

    public bool TrySetLanguage(string installPath, string language)
    {
        if (!_backend.SubKeyExists(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath) ||
            !_backend.SubKeyExists(RegistryHive.CurrentUser, RegistryView.Default, GameRegistryPath))
        {
            return false;
        }

        var languageService = new Ra3LanguageService();
        if (!languageService.GetValidLanguages(installPath).Contains(language, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "language", Ra3LanguageService.GetDisplayLanguage(language), RegistryValueKind.String);
        _backend.SetValue(RegistryHive.CurrentUser, RegistryView.Default, GameRegistryPath, "Language", language, RegistryValueKind.String);
        return true;
    }
}
