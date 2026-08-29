using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Ra3.Tools.Installation;

namespace Ra3.Tools.Interop.Windows;

/// <summary>RA3 游戏窗口查找与定位所需的 Win32 互操作（对应 RA3Bar-RA3Launcher 的 findRA3Window / SetWindowPos 调用）。</summary>
internal static class WindowInterop
{
    /// <summary>在顶层窗口中查找属于指定进程、可见、无属主且类名为 <see cref="Ra3.Tools.Installation.Ra3GameWindow.Ra3WindowClassName"/> 的窗口。</summary>
    /// <param name="processId">游戏进程 ID。</param>
    /// <param name="windowHandle">找到的窗口句柄；未找到为 <see cref="IntPtr.Zero"/>。</param>
    internal static bool TryFindRa3Window(int processId, out IntPtr windowHandle)
    {
        var found = IntPtr.Zero;
        EnumWindowsProc callback = (window, _) =>
        {
            GetWindowThreadProcessId(window, out var ownerProcessId);
            if (ownerProcessId != processId)
            {
                return true;
            }

            // 与 RA3Bar 的 findRA3Window 一致：跳过有属主（弹窗/工具窗）或不可见的窗口。
            if (GetWindow(window, GW_Owner) != IntPtr.Zero || !IsWindowVisible(window))
            {
                return true;
            }

            var className = new StringBuilder(ClassNameBufferSize);
            if (GetClassNameW(window, className, className.Capacity) == 0)
            {
                return true;
            }

            // 标准类名是 RA3 主窗口的 GUID。注意 RA3Bar 作者魔改过自己游戏的类名（首字符 hack 放宽校验），
            // 本库按标准类名完整匹配（忽略大小写）。
            if (!className.ToString().StartsWith(Ra3GameWindow.Ra3WindowClassName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            found = window;
            return false;
        };

        EnumWindows(callback, IntPtr.Zero);
        GC.KeepAlive(callback);
        windowHandle = found;
        return found != IntPtr.Zero;
    }

    /// <summary>发送 WM_NULL（带 SMTO_ERRORONEXIT）确认窗口仍可响应（窗口所属线程已退出时返回 false）。</summary>
    /// <param name="windowHandle">窗口句柄。</param>
    /// <param name="timeoutMilliseconds">发送超时。</param>
    internal static bool IsWindowAlive(IntPtr windowHandle, int timeoutMilliseconds)
    {
        return SendMessageTimeoutW(windowHandle, WM_Null, IntPtr.Zero, IntPtr.Zero, SmtoErrorOnExit, (uint)timeoutMilliseconds, out _) != IntPtr.Zero;
    }

    /// <summary>窗口句柄是否仍然有效（IsWindow，纯存在性检查，不依赖窗口线程是否在响应消息）。</summary>
    /// <param name="windowHandle">窗口句柄。</param>
    internal static bool IsWindowValid(IntPtr windowHandle)
    {
        return IsWindow(windowHandle);
    }

    /// <summary>
    /// 把当前线程的 DPI 感知上下文切到「unaware」，返回原上下文（用 <see cref="ExitDpiScope"/> 恢复）。
    /// RA3 是 DPI unaware 的老程序，窗口调整必须在同一坐标系下进行：宿主若是 DPI 感知进程（如 WPF），
    /// GetWindowRect/GetMonitorInfoW/SetWindowPos 会按物理像素解释坐标，与游戏的虚拟化坐标系混用会算出屏幕外位置
    /// （如 4K@150% 下物理 3840 配 2560 宽虚拟桌面）。系统过旧（无此 API）时返回
    /// <see cref="IntPtr.Zero"/>，调用方按原上下文继续（best effort）。
    /// </summary>
    internal static IntPtr EnterDpiUnawareScope()
    {
        try
        {
            return SetThreadDpiAwarenessContext(DpiAwarenessContextUnaware);
        }
        catch (EntryPointNotFoundException)
        {
            return IntPtr.Zero;
        }
    }

    /// <summary>恢复 <see cref="EnterDpiUnawareScope"/> 之前的线程 DPI 感知上下文。</summary>
    /// <param name="previousContext">EnterDpiUnawareScope 的返回值；为 <see cref="IntPtr.Zero"/> 时不动。</param>
    internal static void ExitDpiScope(IntPtr previousContext)
    {
        if (previousContext != IntPtr.Zero)
        {
            SetThreadDpiAwarenessContext(previousContext);
        }
    }

    /// <summary>返回窗口在屏幕坐标系中的外接矩形。</summary>
    /// <param name="windowHandle">窗口句柄。</param>
    internal static Rect GetWindowRect(IntPtr windowHandle)
    {
        if (!GetWindowRectNative(windowHandle, out var rect))
        {
            throw new Win32Exception();
        }

        return rect;
    }

    /// <summary>移动窗口（保留尺寸），并按其 z-order 语义处理：默认取消置顶（HWND_NOTOPMOST），preserveZOrder 时保持原 z-order（-allowAlwaysOnTop）。</summary>
    /// <param name="windowHandle">窗口句柄。</param>
    /// <param name="left">新的屏幕 X 坐标。</param>
    /// <param name="top">新的屏幕 Y 坐标。</param>
    /// <param name="preserveZOrder">是否保留原 z-order（对应 RA3Bar 的 -allowAlwaysOnTop → SWP_NOZORDER）。</param>
    internal static void SetWindowPosition(IntPtr windowHandle, int left, int top, bool preserveZOrder)
    {
        var flags = SwpAsyncWindowPos | SwpNoSize | (preserveZOrder ? SwpNoZOrder : 0);
        SetWindowPos(windowHandle, new IntPtr(HwndNotTopmost), left, top, 0, 0, flags);
    }

    /// <summary>桌面窗口句柄（用于计算屏幕尺寸）。</summary>
    internal static IntPtr GetDesktopWindowHandle() => GetDesktopWindow();

    /// <summary>
    /// 窗口当前所在显示器的完整矩形（rcMonitor，不含任务栏裁剪）。
    /// RA3Bar 用整块桌面（GetDesktopWindow）做居中基准，多显示器带负原点时会把窗口顶出屏幕；
    /// 这里改用窗口所在显示器，单显示器行为与 RA3Bar 完全一致，取不到显示器时回退桌面。
    /// </summary>
    /// <param name="windowHandle">窗口句柄。</param>
    internal static Rect GetMonitorRect(IntPtr windowHandle)
    {
        var monitor = MonitorFromWindow(windowHandle, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return GetWindowRect(GetDesktopWindowHandle());
        }

        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfoW(monitor, ref info))
        {
            throw new Win32Exception();
        }

        return info.Monitor;
    }

    /// <summary>窗口矩形（屏幕坐标）。</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    /// <summary>MONITORINFO 结构（Size 初始化后传入 <c>GetMonitorInfoW</c>）。</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect Work;
        public uint Flags;
    }

    private const int ClassNameBufferSize = 256;
    private const uint GW_Owner = 4;
    private const uint WM_Null = 0x0000;
    private const uint SmtoErrorOnExit = 0x0020;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpAsyncWindowPos = 0x4000;
    private const int HwndNotTopmost = -2;
    private const uint MonitorDefaultToNearest = 0x0002;

    private static readonly IntPtr DpiAwarenessContextUnaware = new(-1);

    private delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr window, uint command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassNameW(IntPtr window, [Out] StringBuilder className, int maxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeoutW(IntPtr window, uint message, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr result);

    [DllImport("user32.dll", EntryPoint = "GetWindowRect", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRectNative(IntPtr window, out Rect rect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfoW(IntPtr monitor, ref MonitorInfo info);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
}
