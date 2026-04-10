using System.Runtime.InteropServices;

namespace Ra3.Tools.Versioning;

public static class SystemInfo
{
    private static readonly string _osArchitecture = RuntimeInformation.OSArchitecture.ToString();
    private static readonly bool _isArm = _osArchitecture.Contains("ARM", StringComparison.OrdinalIgnoreCase);
    private static readonly Version? _osVersion = Environment.OSVersion.Version;

    public static bool IsArm => _isArm;

    public static string GetSystemVersion()
    {
        if (_osVersion?.Build > 20000)
        {
            return "Windows 11";
        }
        if (_osVersion?.Build > 10240)
        {
            return "Windows 10";
        }
        return "Windows 7, 8, 8.1";
    }
}
