using System.Runtime.InteropServices;

namespace Ra3.Tools.Graphics;

/// <summary>枚举当前主显示器的当前/支持显示模式（Win32 EnumDisplaySettings）。</summary>
public sealed class WindowsDisplayService
{
    /// <summary>返回当前显示模式；查询失败返回 null。</summary>
    public DisplayMode? GetCurrentDisplayMode()
    {
        var mode = CreateDeviceMode();
        return EnumDisplaySettings(null, EnumCurrentSettings, ref mode) ? Convert(mode) : null;
    }

    /// <summary>返回主显示器支持的全部显示模式。</summary>
    public IReadOnlyList<DisplayMode> GetSupportedDisplayModes()
    {
        var result = new List<DisplayMode>();
        var mode = CreateDeviceMode();
        var index = 0;
        while (EnumDisplaySettings(null, index, ref mode))
        {
            result.Add(Convert(mode));
            index++;
        }

        return result;
    }

    /// <summary>判断指定分辨率是否为当前显示器支持的模式。</summary>
    /// <param name="width">宽度（像素）。</param>
    /// <param name="height">高度（像素）。</param>
    public bool IsSupported(int width, int height)
    {
        return GetSupportedDisplayModes().Any(mode => mode.Width == width && mode.Height == height);
    }

    private static DisplayMode Convert(DeviceMode mode)
    {
        return new DisplayMode(mode.dmPelsWidth, mode.dmPelsHeight, mode.dmDisplayFrequency, mode.dmBitsPerPel);
    }

    private static DeviceMode CreateDeviceMode()
    {
        return new DeviceMode
        {
            DeviceName = new string('\0', 32),
            FormName = new string('\0', 32),
            dmSize = (short)Marshal.SizeOf<DeviceMode>()
        };
    }

    private const int EnumCurrentSettings = -1;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DeviceMode deviceMode);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DeviceMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public Point dmPosition;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string FormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }
}
