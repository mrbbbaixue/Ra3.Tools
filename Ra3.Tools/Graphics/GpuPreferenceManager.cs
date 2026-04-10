using System.Text.RegularExpressions;
using Microsoft.Win32;
using Ra3.Tools.Interop.Windows;

namespace Ra3.Tools.Graphics;

public sealed class GpuPreferenceManager
{
    private const string UserGpuPreferencesPath = @"Software\Microsoft\DirectX\UserGpuPreferences";
    private static readonly Regex PreferenceRegex = new(@"GpuPreference=(\d+);", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IRegistryBackend _backend;

    public GpuPreferenceManager() : this(new WindowsRegistryBackend())
    {
    }

    internal GpuPreferenceManager(IRegistryBackend backend)
    {
        _backend = backend;
    }

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
