using Microsoft.Win32;

namespace Ra3.Tools.Interop.Windows;

internal sealed class WindowsRegistryBackend : IRegistryBackend
{
    public object? GetValue(RegistryHive hive, RegistryView view, string subKeyPath, string? name)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        using var key = baseKey.OpenSubKey(subKeyPath, writable: false);
        return key?.GetValue(name);
    }

    public void SetValue(RegistryHive hive, RegistryView view, string subKeyPath, string? name, object value, RegistryValueKind valueKind)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        using var key = baseKey.CreateSubKey(subKeyPath, writable: true);
        key?.SetValue(name, value, valueKind);
    }

    public bool SubKeyExists(RegistryHive hive, RegistryView view, string subKeyPath)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        using var key = baseKey.OpenSubKey(subKeyPath, writable: false);
        return key != null;
    }

    public void DeleteSubKeyTree(RegistryHive hive, RegistryView view, string subKeyPath)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        if (!SubKeyExists(hive, view, subKeyPath))
        {
            return;
        }

        baseKey.DeleteSubKeyTree(subKeyPath);
    }
}
