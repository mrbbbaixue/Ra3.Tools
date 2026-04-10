using Microsoft.Win32;
using Ra3.Tools.Interop.Windows;

namespace Ra3.Tools.Test.TestDoubles;

internal sealed class MemoryRegistryBackend : IRegistryBackend
{
    private readonly Dictionary<(RegistryHive Hive, RegistryView View, string Path, string? Name), object> _values = new();

    public object? GetValue(RegistryHive hive, RegistryView view, string subKeyPath, string? name)
    {
        return _values.TryGetValue((hive, view, subKeyPath, name), out var value) ? value : null;
    }

    public void SetValue(RegistryHive hive, RegistryView view, string subKeyPath, string? name, object value, RegistryValueKind valueKind)
    {
        _values[(hive, view, subKeyPath, name)] = value;
    }

    public bool SubKeyExists(RegistryHive hive, RegistryView view, string subKeyPath)
    {
        return _values.Keys.Any(key =>
            key.Hive == hive &&
            key.View == view &&
            (key.Path.Equals(subKeyPath, StringComparison.OrdinalIgnoreCase) ||
             key.Path.StartsWith(subKeyPath + "\\", StringComparison.OrdinalIgnoreCase)));
    }

    public void DeleteSubKeyTree(RegistryHive hive, RegistryView view, string subKeyPath)
    {
        var keys = _values.Keys.Where(key =>
                key.Hive == hive &&
                key.View == view &&
                (key.Path.Equals(subKeyPath, StringComparison.OrdinalIgnoreCase) ||
                 key.Path.StartsWith(subKeyPath + "\\", StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        foreach (var key in keys)
        {
            _values.Remove(key);
        }
    }
}
