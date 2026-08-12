using System.Diagnostics;
using Ra3.Tools.Interop.Windows;

namespace Ra3.Tools.Installation;

/// <summary>
/// RA3 游戏窗口查找与无边框全屏（-win -fullscreen）位置修正，对应 RA3Bar-RA3Launcher main.cpp 的
/// findRA3Window + SetWindowPos 逻辑：窗口化启动后等待游戏窗口出现，按 -xres/-yres 在桌面上居中，
/// -x/-y 显式指定位置，-allowAlwaysOnTop 时保留原 z-order。
/// </summary>
public static class Ra3GameWindow
{
    /// <summary>RA3 主窗口类名（GUID，findRA3Window 的匹配特征）。</summary>
    public const string Ra3WindowClassName = "41DAF790-16F5-4881-8754-59FD8CF3B8D2";

    /// <summary>
    /// 等待并查找 RA3 游戏主窗口（与 RA3Bar 一致：进程存活 + 输入空闲 + 可见无属主 + 类名匹配 + 窗口可响应）。
    /// RA3Bar 会一直轮询直到游戏退出；本库默认 5 秒超时（<paramref name="timeoutMilliseconds"/> ≤ 0 时与 RA3Bar 一致不设超时）。
    /// </summary>
    /// <param name="process">已启动的游戏进程。</param>
    /// <param name="windowHandle">找到的游戏窗口句柄；超时或进程退出为 <see cref="IntPtr.Zero"/>。</param>
    /// <param name="timeoutMilliseconds">查找超时（毫秒），≤ 0 表示不设超时。</param>
    public static bool TryFindWindow(Process process, out IntPtr windowHandle, int timeoutMilliseconds = 5000)
    {
        ArgumentNullException.ThrowIfNull(process);
        windowHandle = IntPtr.Zero;

        var deadline = timeoutMilliseconds <= 0 ? long.MaxValue : Environment.TickCount64 + timeoutMilliseconds;
        while (Environment.TickCount64 < deadline)
        {
            // 进程已退出 → 窗口不会再出现。
            if (process.WaitForExit(Interval))
            {
                return false;
            }

            // 游戏尚未进入输入空闲（窗口消息循环未就绪）。
            if (!process.WaitForInputIdle(1))
            {
                continue;
            }

            if (!WindowInterop.TryFindRa3Window(process.Id, out var handle))
            {
                continue;
            }

            // 窗口所属线程可能刚退出（SendMessageTimeout 带 SMTO_ERRORONEXIT 时返回 0）。
            if (!WindowInterop.IsWindowAlive(handle, Interval))
            {
                continue;
            }

            windowHandle = handle;
            return true;
        }

        return false;
    }

    /// <summary>按调整项移动游戏窗口：无边框全屏时按分辨率居中于桌面，-x/-y 显式覆盖；默认取消置顶。</summary>
    /// <param name="windowHandle">游戏窗口句柄。</param>
    /// <param name="adjustment">窗口调整项（见 <see cref="ParseFromArguments"/>）。</param>
    public static void AdjustPosition(IntPtr windowHandle, Ra3WindowAdjustment adjustment)
    {
        ArgumentNullException.ThrowIfNull(adjustment);
        if (windowHandle == IntPtr.Zero)
        {
            throw new ArgumentException("窗口句柄不能为 0。", nameof(windowHandle));
        }

        var current = WindowInterop.GetWindowRect(windowHandle);
        var desktop = WindowInterop.GetWindowRect(WindowInterop.GetDesktopWindowHandle());
        var (left, top) = ComputePosition(current, desktop, adjustment);
        WindowInterop.SetWindowPosition(windowHandle, left, top, adjustment.AllowAlwaysOnTop);
    }

    /// <summary>
    /// 从启动参数解析窗口调整项（对应 RA3Bar 的 -win/-fullscreen/-xres/-yres/-x/-y/-allowAlwaysOnTop）。
    /// 参数名大小写不敏感、与 -runver 等一样转发给游戏（本库不做剔除）；数值不可解析时忽略该项。
    /// </summary>
    /// <param name="arguments">启动附加参数（<see cref="Ra3LaunchOptions.Arguments"/>）。</param>
    /// <param name="windowed">是否已按窗口化模式启动（<see cref="Ra3LaunchMode.Windowed"/>）；无 -win 参数时也视为窗口化。</param>
    /// <returns>调整项；未以窗口化启动时为 null。</returns>
    internal static Ra3WindowAdjustment? ParseFromArguments(string? arguments, bool windowed = false)
    {
        var hasWindowed = windowed;
        var fullscreen = false;
        var allowAlwaysOnTop = false;
        int? resolutionWidth = null;
        int? resolutionHeight = null;
        int? x = null;
        int? y = null;

        if (!string.IsNullOrWhiteSpace(arguments))
        {
            var tokens = Ra3CommandLine.SplitArgs(arguments).ToArray();
            for (var index = 0; index < tokens.Length; index++)
            {
                var token = tokens[index];
                if (token.Equals("-win", StringComparison.OrdinalIgnoreCase))
                {
                    hasWindowed = true;
                    continue;
                }

                if (token.Equals("-fullscreen", StringComparison.OrdinalIgnoreCase))
                {
                    fullscreen = true;
                    continue;
                }

                if (token.Equals("-allowAlwaysOnTop", StringComparison.OrdinalIgnoreCase))
                {
                    allowAlwaysOnTop = true;
                    continue;
                }

                // 注意：out 参数只能在对应 token 匹配时才调用（失败也会把 out 置 null，
                // 若在 || 链里对每个 token 都调用会把已解析的值清掉）。
                if (token.Equals("-xres", StringComparison.OrdinalIgnoreCase))
                {
                    TryParseValue(tokens, ref index, out resolutionWidth);
                    continue;
                }

                if (token.Equals("-yres", StringComparison.OrdinalIgnoreCase))
                {
                    TryParseValue(tokens, ref index, out resolutionHeight);
                    continue;
                }

                if (token.Equals("-x", StringComparison.OrdinalIgnoreCase))
                {
                    TryParseValue(tokens, ref index, out x);
                    continue;
                }

                if (token.Equals("-y", StringComparison.OrdinalIgnoreCase))
                {
                    TryParseValue(tokens, ref index, out y);
                }
            }
        }

        return hasWindowed
            ? new Ra3WindowAdjustment(fullscreen, resolutionWidth, resolutionHeight, x, y, allowAlwaysOnTop)
            : null;
    }

    /// <summary>计算调整后的窗口左上角位置（纯函数，与 RA3Bar 的 rect.left/rect.top 计算一致）。</summary>
    /// <param name="current">窗口当前矩形。</param>
    /// <param name="desktop">桌面矩形。</param>
    /// <param name="adjustment">窗口调整项。</param>
    internal static (int Left, int Top) ComputePosition(WindowInterop.Rect current, WindowInterop.Rect desktop, Ra3WindowAdjustment adjustment)
    {
        var left = current.Left;
        var top = current.Top;

        // 无边框全屏：按 -xres/-yres 把窗口居中于桌面（整数除法，与 C++ 一致）。
        if (adjustment.Fullscreen)
        {
            if (adjustment.ResolutionWidth is { } width)
            {
                left = (desktop.Width - width) / 2;
            }

            if (adjustment.ResolutionHeight is { } height)
            {
                top = (desktop.Height - height) / 2;
            }
        }

        // -x/-y 显式指定位置，覆盖居中结果。
        if (adjustment.X is { } positionX)
        {
            left = positionX;
        }

        if (adjustment.Y is { } positionY)
        {
            top = positionY;
        }

        return (left, top);
    }

    private static void TryParseValue(IReadOnlyList<string> tokens, ref int index, out int? value)
    {
        value = null;
        if (index + 1 >= tokens.Count || !int.TryParse(tokens[index + 1], out var parsed))
        {
            return;
        }

        value = parsed;
        index++;
    }

    /// <summary>轮询间隔（毫秒），与 RA3Bar 的 interval=499 一致。</summary>
    private const int Interval = 499;
}

/// <summary>RA3 游戏窗口位置调整项（对应 RA3Bar 的 -fullscreen/-xres/-yres/-x/-y/-allowAlwaysOnTop）。</summary>
/// <param name="Fullscreen">是否无边框全屏（-fullscreen，配合 <see cref="Ra3LaunchMode.Windowed"/> 使用）。</param>
/// <param name="ResolutionWidth">请求的分辨率宽度（-xres），用于桌面居中。</param>
/// <param name="ResolutionHeight">请求的分辨率高度（-yres），用于桌面居中。</param>
/// <param name="X">窗口屏幕 X 坐标（-x），覆盖居中结果。</param>
/// <param name="Y">窗口屏幕 Y 坐标（-y），覆盖居中结果。</param>
/// <param name="AllowAlwaysOnTop">保留原 z-order（-allowAlwaysOnTop，不加 SWP_NOZORDER 时默认取消置顶）。</param>
public sealed record Ra3WindowAdjustment(bool Fullscreen,
                                         int? ResolutionWidth,
                                         int? ResolutionHeight,
                                         int? X,
                                         int? Y,
                                         bool AllowAlwaysOnTop);
