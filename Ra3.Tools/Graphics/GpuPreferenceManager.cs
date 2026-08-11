using System.Text.RegularExpressions;
using Microsoft.Win32;
using Ra3.Tools.Interop.Windows;

namespace Ra3.Tools.Graphics;

/// <summary>读写 DirectX 用户 GPU 偏好注册表（UserGpuPreferences），用于强制 RA3 使用独显。</summary>
public sealed class GpuPreferenceManager
{
    private const string UserGpuPreferencesPath = @"Software\Microsoft\DirectX\UserGpuPreferences";
    private static readonly Regex PreferenceRegex = new(@"GpuPreference=(\d+);", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IRegistryBackend _backend;

    /// <summary>使用默认 Windows 注册表后端创建实例。</summary>
    public GpuPreferenceManager() : this(new WindowsRegistryBackend())
    {
    }

    internal GpuPreferenceManager(IRegistryBackend backend)
    {
        _backend = backend;
    }

    /// <summary>读取指定可执行文件当前的 GPU 偏好；未设置时返回 null。</summary>
    /// <param name="executablePath">可执行文件路径。</param>
    public GpuPreference? Get(string executablePath)
    {
        var fullPath = Path.GetFullPath(executablePath);
        var rawValue = _backend.GetValue(RegistryHive.CurrentUser, RegistryView.Default, UserGpuPreferencesPath, fullPath) as string;
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        var match = PreferenceRegex.Match(rawValue);
        return match.Success && Enum.TryParse<GpuPreference>(match.Groups[1].Value, out var preference) ? preference : null;
    }

    /// <summary>设置指定可执行文件的 GPU 偏好。</summary>
    /// <param name="executablePath">可执行文件路径。</param>
    /// <param name="preference">目标偏好。</param>
    public void Set(string executablePath, GpuPreference preference)
    {
        var fullPath = Path.GetFullPath(executablePath);
        _backend.SetValue(RegistryHive.CurrentUser,
                          RegistryView.Default,
                          UserGpuPreferencesPath,
                          fullPath,
                          $"GpuPreference={(int)preference};",
                          RegistryValueKind.String);
    }
}
