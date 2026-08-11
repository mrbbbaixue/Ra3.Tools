using Microsoft.Win32;
using Ra3.Tools.Interop.Windows;

namespace Ra3.Tools.Installation;

/// <summary>
/// 读写 RA3 游戏注册表（HKLM/HKCU 的 Electronic Arts\Red Alert 3 键），
/// 支持安装路径、语言、地图同步与完整重建。
/// </summary>
public sealed class Ra3Registry
{
    private const string GameRegistryPath = @"Software\Electronic Arts\Electronic Arts\Red Alert 3";
    private const string ErgcRegistryPath = @"Software\Electronic Arts\Electronic Arts\Red Alert 3\ergc";

    private readonly IRegistryBackend _backend;

    /// <summary>使用默认 Windows 注册表后端创建实例。</summary>
    public Ra3Registry() : this(new WindowsRegistryBackend())
    {
    }

    internal Ra3Registry(IRegistryBackend backend)
    {
        _backend = backend;
    }

    /// <summary>读取注册表中的 Install Dir；未设置返回空字符串。</summary>
    public string GetInstallPath()
    {
        return _backend.GetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "Install Dir") as string ?? string.Empty;
    }

    /// <summary>写入注册表中的 Install Dir（需要管理员权限）。</summary>
    /// <param name="path">安装根目录。</param>
    public void SetInstallPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "Install Dir", fullPath, RegistryValueKind.String);
    }

    /// <summary>逐项检查注册表完整性，返回第一个发现的问题；全部正确返回 <see cref="RegistryStatus.Correct"/>。</summary>
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

    /// <summary>启用本地用户地图同步（同时写入 UseLocalUserMaps / UseLocalUserMap 两个键）。</summary>
    public void EnableMapSync()
    {
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "UseLocalUserMaps", 0, RegistryValueKind.DWord);
        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "UseLocalUserMap", 0, RegistryValueKind.DWord);
    }

    /// <summary>删除 HKLM/HKCU 下全部 RA3 注册表键。</summary>
    public void Clear()
    {
        _backend.DeleteSubKeyTree(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath);
        _backend.DeleteSubKeyTree(RegistryHive.CurrentUser, RegistryView.Default, GameRegistryPath);
    }

    /// <summary>清空并重建完整的 RA3 注册表（安装路径、产品信息、CD 密钥、地图同步与语言）。</summary>
    /// <param name="installPath">安装根目录。</param>
    /// <param name="key">CD 密钥（写入 ergc 子键）。</param>
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

    /// <summary>切换游戏语言（需安装支持该语言）；语言无效或键缺失返回 false。</summary>
    /// <param name="installPath">安装根目录。</param>
    /// <param name="language">语言代码（如 "english"、"chinese_s"）。</param>
    public bool TrySetLanguage(string installPath, string language)
    {
        if (!_backend.SubKeyExists(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath) ||
            !_backend.SubKeyExists(RegistryHive.CurrentUser, RegistryView.Default, GameRegistryPath))
        {
            return false;
        }

        var languageCatalog = new Ra3LanguageCatalog();
        if (!languageCatalog.GetValidLanguages(installPath).Contains(language, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        _backend.SetValue(RegistryHive.LocalMachine, RegistryView.Registry32, GameRegistryPath, "language", Ra3LanguageCatalog.GetDisplayLanguage(language), RegistryValueKind.String);
        _backend.SetValue(RegistryHive.CurrentUser, RegistryView.Default, GameRegistryPath, "Language", language, RegistryValueKind.String);
        return true;
    }
}
