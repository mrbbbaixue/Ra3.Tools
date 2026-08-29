using System.ComponentModel;
using System.Diagnostics;
using Ra3.Tools.Interop.Windows;

namespace Ra3.Tools.Installation;

/// <summary>
/// RA3 游戏窗口查找与无边框全屏（-win -fullscreen）位置修正，对应 RA3Bar-RA3Launcher main.cpp 的
/// findRA3Window + SetWindowPos 逻辑：窗口化启动后等游戏窗口出现，进程存活 + 输入空闲 + 窗口即时响应
/// 三重条件都满足视为游戏初始化完毕，把窗口按<strong>实际尺寸</strong>居中于其所在显示器，
/// -x/-y 显式指定位置，-allowAlwaysOnTop 时保留原 z-order。
/// 与 RA3Bar 的三处有意差异：居中不用 -xres/-yres（那是游戏物理像素语义，与 DPI 虚拟化坐标系混用会算出屏幕外位置，
/// 如 4K@150% 下 -xres 3840 配 2560 宽虚拟桌面得 -640）、居中基准用窗口所在显示器而非整块桌面（多显示器负原点）、
/// 另加有界稳定观察应对初始化后段的窗口改尺寸。调整期间线程 DPI 上下文钉在 unaware（与游戏一致）。
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

    /// <summary>按调整项移动游戏窗口：无边框全屏时把窗口按实际尺寸居中于所在显示器，-x/-y 显式覆盖；默认取消置顶。</summary>
    /// <param name="windowHandle">游戏窗口句柄。</param>
    /// <param name="adjustment">窗口调整项（见 <see cref="ParseFromArguments"/>）。</param>
    /// <param name="stabilizationMilliseconds">居中后的稳定观察时长（毫秒，默认 3 秒）：游戏初始化后段可能还会改窗口
    /// 尺寸（默认分辨率 → 用户设置分辨率），尺寸一变即按新尺寸重新居中，连续稳定若干轮或超时即止；≤ 0 只调整一次。
    /// 仅无边框全屏（<see cref="Ra3WindowAdjustment.Fullscreen"/>）生效。</param>
    /// <remarks>整个调整过程把线程 DPI 上下文切到 unaware（与游戏同一坐标系，见
    /// <see cref="WindowInterop.EnterDpiUnawareScope"/>），并在 finally 中恢复。</remarks>
    public static void AdjustPosition(IntPtr windowHandle, Ra3WindowAdjustment adjustment, int stabilizationMilliseconds = DefaultStabilizationMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(adjustment);
        if (windowHandle == IntPtr.Zero)
        {
            throw new ArgumentException("窗口句柄不能为 0。", nameof(windowHandle));
        }

        var previousContext = WindowInterop.EnterDpiUnawareScope();
        try
        {
            var adjusted = AdjustOnce(windowHandle, adjustment);
            if (adjustment.Fullscreen)
            {
                Stabilize(windowHandle, adjustment, adjusted.Width, adjusted.Height, stabilizationMilliseconds);
            }
        }
        finally
        {
            WindowInterop.ExitDpiScope(previousContext);
        }
    }

    /// <summary>执行一次「读当前矩形 → 按所在显示器计算位置 → 移动窗口」，返回移动结果（含参与居中的窗口尺寸）。</summary>
    private static (int Left, int Top, int Width, int Height) AdjustOnce(IntPtr windowHandle, Ra3WindowAdjustment adjustment)
    {
        var current = WindowInterop.GetWindowRect(windowHandle);
        var anchor = WindowInterop.GetMonitorRect(windowHandle);
        var (left, top) = ComputePosition(current, anchor, adjustment);
        WindowInterop.SetWindowPosition(windowHandle, left, top, adjustment.AllowAlwaysOnTop);
        return (left, top, current.Width, current.Height);
    }

    /// <summary>稳定观察：游戏初始化后段可能还会改窗口尺寸，尺寸一变即按新尺寸重新居中；连续稳定或超时即止。</summary>
    private static void Stabilize(IntPtr windowHandle, Ra3WindowAdjustment adjustment, int lastWidth, int lastHeight, int stabilizationMilliseconds)
    {
        if (stabilizationMilliseconds <= 0)
        {
            return;
        }

        var deadline = Environment.TickCount64 + stabilizationMilliseconds;
        var stableRounds = 0;
        while (Environment.TickCount64 < deadline)
        {
            Thread.Sleep(Interval);

            // 窗口已销毁（游戏退出/崩溃）→ 无需继续跟踪。
            if (!WindowInterop.IsWindowValid(windowHandle))
            {
                return;
            }

            WindowInterop.Rect rect;
            try
            {
                rect = WindowInterop.GetWindowRect(windowHandle);
            }
            catch (Win32Exception)
            {
                // 读矩形失败（窗口恰在销毁）→ 视同退出跟踪。
                return;
            }

            if (rect.Width == lastWidth && rect.Height == lastHeight)
            {
                if (++stableRounds >= StableRounds)
                {
                    return;
                }

                continue;
            }

            var adjusted = AdjustOnce(windowHandle, adjustment);
            lastWidth = adjusted.Width;
            lastHeight = adjusted.Height;
            stableRounds = 0;
        }
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

    /// <summary>计算调整后的窗口左上角位置（纯函数）。</summary>
    /// <param name="current">窗口当前矩形：无边框全屏时用其<strong>实际尺寸</strong>参与居中——三重等待通过后
    /// 它就是用户设置的最终分辨率，且与显示器矩形同一坐标系；不使用 -xres/-yres 计算（见类型文档）。</param>
    /// <param name="anchor">居中基准矩形（<see cref="AdjustPosition"/> 传入窗口所在显示器，含原点偏移）。</param>
    /// <param name="adjustment">窗口调整项（-xres/-yres 仅转发给游戏进程，不参与居中）。</param>
    internal static (int Left, int Top) ComputePosition(WindowInterop.Rect current, WindowInterop.Rect anchor, Ra3WindowAdjustment adjustment)
    {
        var left = current.Left;
        var top = current.Top;

        // 无边框全屏：按窗口实际尺寸在所在显示器内居中（整数除法；尺寸==显示器时正好铺满，无缝隙）。
        if (adjustment.Fullscreen)
        {
            left = anchor.Left + (anchor.Width - current.Width) / 2;
            top = anchor.Top + (anchor.Height - current.Height) / 2;
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

    /// <summary>稳定观察中连续多少轮尺寸不变视为已稳定。</summary>
    private const int StableRounds = 3;

    /// <summary>AdjustPosition 默认的稳定观察时长（毫秒）。</summary>
    private const int DefaultStabilizationMilliseconds = 3000;
}

/// <summary>RA3 游戏窗口位置调整项（对应 RA3Bar 的 -fullscreen/-xres/-yres/-x/-y/-allowAlwaysOnTop）。</summary>
/// <param name="Fullscreen">是否无边框全屏（-fullscreen，配合 <see cref="Ra3LaunchMode.Windowed"/> 使用）。</param>
/// <param name="ResolutionWidth">请求的分辨率宽度（-xres）。仅原样转发给游戏进程，<strong>不参与居中计算</strong>
/// （居中用窗口实际尺寸，见 <see cref="Ra3GameWindow.ComputePosition"/>）。</param>
/// <param name="ResolutionHeight">请求的分辨率高度（-yres）。仅原样转发给游戏进程，不参与居中计算。</param>
/// <param name="X">窗口屏幕 X 坐标（-x），覆盖居中结果。</param>
/// <param name="Y">窗口屏幕 Y 坐标（-y），覆盖居中结果。</param>
/// <param name="AllowAlwaysOnTop">保留原 z-order（-allowAlwaysOnTop，不加 SWP_NOZORDER 时默认取消置顶）。</param>
public sealed record Ra3WindowAdjustment(bool Fullscreen,
                                         int? ResolutionWidth,
                                         int? ResolutionHeight,
                                         int? X,
                                         int? Y,
                                         bool AllowAlwaysOnTop);
