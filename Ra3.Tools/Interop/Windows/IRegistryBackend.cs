using Microsoft.Win32;

namespace Ra3.Tools.Interop.Windows;

internal interface IRegistryBackend
{
    object? GetValue(RegistryHive hive, RegistryView view, string subKeyPath, string? name);

    void SetValue(RegistryHive hive, RegistryView view, string subKeyPath, string? name, object value, RegistryValueKind valueKind);

    bool SubKeyExists(RegistryHive hive, RegistryView view, string subKeyPath);

    void DeleteSubKeyTree(RegistryHive hive, RegistryView view, string subKeyPath);
}
