using System.Runtime.InteropServices;

namespace Ra3.Tools.Versioning;

/// <summary>当前系统信息（架构与 Windows 版本判断）。</summary>
public static class SystemInfo
{
    private static readonly string _osArchitecture = RuntimeInformation.OSArchitecture.ToString();
    private static readonly bool _isArm = _osArchitecture.Contains("ARM", StringComparison.OrdinalIgnoreCase);
    private static readonly Version? _osVersion = Environment.OSVersion.Version;

    /// <summary>是否为 ARM 架构（ARM32/ARM64）。</summary>
    public static bool IsArm => _isArm;

    /// <summary>返回 Windows 版本描述（"Windows 11" / "Windows 10" / "Windows 7, 8, 8.1"）。</summary>
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
