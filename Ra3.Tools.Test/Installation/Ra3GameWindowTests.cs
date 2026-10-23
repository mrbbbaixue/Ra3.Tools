using Ra3.Tools.Installation;
using Ra3.Tools.Interop.Windows;

namespace Ra3.Tools.Test.Installation;

public sealed class Ra3GameWindowTests
{
    [Fact]
    public void StartupReadiness_DoesNotAcceptFirstOrRepeatedResponsiveWindow()
    {
        var readiness = new Ra3GameWindow.StartupReadiness();
        var handle = new IntPtr(1);

        // Even more unchanged responsive samples than the position stabilization loop accepts
        // cannot replace observing the actual startup unresponsive phase.
        for (var sample = 0; sample < 10; sample++)
        {
            Assert.False(readiness.Observe(handle, responsive: true));
        }
    }

    [Fact]
    public void StartupReadiness_WaitsForRecoveryAfterStartupStopsResponding()
    {
        var readiness = new Ra3GameWindow.StartupReadiness();
        var handle = new IntPtr(1);

        Assert.False(readiness.Observe(handle, responsive: true));
        Assert.False(readiness.Observe(handle, responsive: false));
        Assert.False(readiness.Observe(handle, responsive: false));
        Assert.True(readiness.Observe(handle, responsive: true));
    }

    [Fact]
    public void StartupReadiness_CanFirstObserveWindowWhileUnresponsive()
    {
        var readiness = new Ra3GameWindow.StartupReadiness();
        var handle = new IntPtr(1);

        Assert.False(readiness.Observe(handle, responsive: false));
        Assert.True(readiness.Observe(handle, responsive: true));
        // A later input-idle check may not pass immediately; do not lose the observed phase.
        Assert.True(readiness.Observe(handle, responsive: true));
    }

    [Fact]
    public void StartupReadiness_DoesNotTransferUnresponsiveStateToReplacementWindow()
    {
        var readiness = new Ra3GameWindow.StartupReadiness();

        Assert.False(readiness.Observe(new IntPtr(1), responsive: false));
        Assert.False(readiness.Observe(new IntPtr(2), responsive: true));
        Assert.False(readiness.Observe(new IntPtr(2), responsive: false));
        Assert.True(readiness.Observe(new IntPtr(2), responsive: true));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StartupReadiness_MissingOrDestroyedWindowResetsObservation(bool responsive)
    {
        var readiness = new Ra3GameWindow.StartupReadiness();
        var handle = new IntPtr(1);

        Assert.False(readiness.Observe(handle, responsive: false));
        Assert.False(readiness.Observe(IntPtr.Zero, responsive));
        Assert.False(readiness.Observe(handle, responsive: true));
    }

    [Fact]
    public void StartupReadiness_PositionUsesRecoveredWindowSizeNotEarlyStableSize()
    {
        var readiness = new Ra3GameWindow.StartupReadiness();
        var handle = new IntPtr(1);
        var monitor = new WindowInterop.Rect { Right = 2560, Bottom = 1440 };
        var early = new WindowInterop.Rect { Left = 40, Top = 30, Right = 1320, Bottom = 750 };
        var recovered = new WindowInterop.Rect { Left = 40, Top = 30, Right = 2600, Bottom = 1470 };
        var adjustment = Ra3GameWindow.ParseFromArguments("-win -fullscreen -xres 3840 -yres 2160")!;
        (int Left, int Top)? position = null;

        // RA3 may keep an early size long enough to fool size-only stabilization.
        for (var sample = 0; sample < 10; sample++)
        {
            if (readiness.Observe(handle, responsive: true))
            {
                position = Ra3GameWindow.ComputePosition(early, monitor, adjustment);
            }
        }

        Assert.Null(position);
        Assert.False(readiness.Observe(handle, responsive: false));
        Assert.True(readiness.Observe(handle, responsive: true));
        position = Ra3GameWindow.ComputePosition(recovered, monitor, adjustment);

        Assert.Equal((0, 0), position.Value);
    }

    [Fact]
    public void ParseFromArguments_ReadsBorderlessFullscreenOptions()
    {
        var adjustment = Ra3GameWindow.ParseFromArguments("-win -fullscreen -xres 1920 -yres 1080");

        Assert.NotNull(adjustment);
        Assert.True(adjustment!.Fullscreen);
        Assert.Equal(1920, adjustment.ResolutionWidth);
        Assert.Equal(1080, adjustment.ResolutionHeight);
        Assert.False(adjustment.AllowAlwaysOnTop);
    }

    [Fact]
    public void ParseFromArguments_IsCaseInsensitive()
    {
        var adjustment = Ra3GameWindow.ParseFromArguments("-WIN -FULLSCREEN -XRES 1920");

        Assert.NotNull(adjustment);
        Assert.True(adjustment!.Fullscreen);
        Assert.Equal(1920, adjustment.ResolutionWidth);
    }

    [Fact]
    public void ParseFromArguments_ReadsExplicitPositionAndZOrder()
    {
        var adjustment = Ra3GameWindow.ParseFromArguments("-x 100 -y 200 -allowAlwaysOnTop", windowed: true);

        Assert.NotNull(adjustment);
        Assert.Equal(100, adjustment!.X);
        Assert.Equal(200, adjustment.Y);
        Assert.True(adjustment.AllowAlwaysOnTop);
        Assert.False(adjustment.Fullscreen);
    }

    [Fact]
    public void ParseFromArguments_WindowedLaunchWithoutArgsStillAdjusts()
    {
        var adjustment = Ra3GameWindow.ParseFromArguments(string.Empty, windowed: true);

        Assert.NotNull(adjustment);
        Assert.False(adjustment!.Fullscreen);
        Assert.Null(adjustment.X);
    }

    [Fact]
    public void ParseFromArguments_ReturnsNullWhenNotWindowed()
    {
        var adjustment = Ra3GameWindow.ParseFromArguments("-fullscreen -xres 1920");

        Assert.Null(adjustment);
    }

    [Fact]
    public void ParseFromArguments_IgnoresMissingOrInvalidValues()
    {
        var missingValue = Ra3GameWindow.ParseFromArguments("-win -xres");
        var invalidValue = Ra3GameWindow.ParseFromArguments("-win -xres abc");

        Assert.Null(missingValue!.ResolutionWidth);
        Assert.Null(invalidValue!.ResolutionWidth);
    }

    [Fact]
    public void ParseFromArguments_ParsesBothPositionAndResolution()
    {
        var adjustment = Ra3GameWindow.ParseFromArguments("-win -x 1 -xres 2 -y 3 -yres 4");

        Assert.Equal(1, adjustment!.X);
        Assert.Equal(2, adjustment.ResolutionWidth);
        Assert.Equal(3, adjustment.Y);
        Assert.Equal(4, adjustment.ResolutionHeight);
    }

    [Fact]
    public void ComputePosition_CentersBorderlessFullscreenWindowOnDesktop()
    {
        var current = new WindowInterop.Rect { Left = 0, Top = 0, Right = 1920, Bottom = 1080 };
        var desktop = new WindowInterop.Rect { Left = 0, Top = 0, Right = 2560, Bottom = 1440 };
        var adjustment = new Ra3WindowAdjustment(Fullscreen: true, 1920, 1080, null, null, AllowAlwaysOnTop: false);

        var (left, top) = Ra3GameWindow.ComputePosition(current, desktop, adjustment);

        Assert.Equal((2560 - 1920) / 2, left);
        Assert.Equal((1440 - 1080) / 2, top);
    }

    [Fact]
    public void ComputePosition_UsesIntegerDivision()
    {
        var current = new WindowInterop.Rect { Left = 0, Top = 0, Right = 1366, Bottom = 768 };
        var desktop = new WindowInterop.Rect { Left = 0, Top = 0, Right = 1920, Bottom = 1080 };
        var adjustment = new Ra3WindowAdjustment(Fullscreen: true, 1366, 768, null, null, AllowAlwaysOnTop: false);

        var (left, _) = Ra3GameWindow.ComputePosition(current, desktop, adjustment);

        Assert.Equal((1920 - 1366) / 2, left);
    }

    [Fact]
    public void ComputePosition_ExplicitPositionOverridesCentering()
    {
        var current = new WindowInterop.Rect { Left = 0, Top = 0, Right = 1920, Bottom = 1080 };
        var desktop = new WindowInterop.Rect { Left = 0, Top = 0, Right = 2560, Bottom = 1440 };
        var adjustment = new Ra3WindowAdjustment(Fullscreen: true, 1920, 1080, X: 0, Y: 0, AllowAlwaysOnTop: false);

        var (left, top) = Ra3GameWindow.ComputePosition(current, desktop, adjustment);

        Assert.Equal(0, left);
        Assert.Equal(0, top);
    }

    [Fact]
    public void ComputePosition_KeepsCurrentPositionWithoutAdjustment()
    {
        var current = new WindowInterop.Rect { Left = 42, Top = 24, Right = 1962, Bottom = 1104 };
        var desktop = new WindowInterop.Rect { Left = 0, Top = 0, Right = 2560, Bottom = 1440 };
        var adjustment = new Ra3WindowAdjustment(Fullscreen: false, null, null, null, null, AllowAlwaysOnTop: false);

        var (left, top) = Ra3GameWindow.ComputePosition(current, desktop, adjustment);

        Assert.Equal(42, left);
        Assert.Equal(24, top);
    }

    [Fact]
    public void GetWindowRect_DesktopWindow_HasPositiveSize()
    {
        var desktop = WindowInterop.GetDesktopWindowHandle();
        var rect = WindowInterop.GetWindowRect(desktop);

        Assert.True(rect.Width > 0);
        Assert.True(rect.Height > 0);
    }

    [Fact]
    public void GetMonitorRect_DesktopWindow_HasPositiveSize()
    {
        var desktop = WindowInterop.GetDesktopWindowHandle();
        var rect = WindowInterop.GetMonitorRect(desktop);

        Assert.True(rect.Width > 0);
        Assert.True(rect.Height > 0);
    }

    [Fact]
    public void ComputePosition_FullscreenCentersByActualWindowSizeWithoutResolution()
    {
        // 未指定 -xres/-yres 时也按窗口实际尺寸居中，不依赖命令行分辨率。
        var current = new WindowInterop.Rect { Left = 42, Top = 24, Right = 1962, Bottom = 1104 };
        var monitor = new WindowInterop.Rect { Left = 0, Top = 0, Right = 2560, Bottom = 1440 };
        var adjustment = new Ra3WindowAdjustment(Fullscreen: true, null, null, null, null, AllowAlwaysOnTop: false);

        var (left, top) = Ra3GameWindow.ComputePosition(current, monitor, adjustment);

        Assert.Equal((2560 - 1920) / 2, left);
        Assert.Equal((1440 - 1080) / 2, top);
    }

    [Fact]
    public void ComputePosition_HidpiPhysicalResolutionDoesNotPushWindowOffScreen()
    {
        // HiDPI 回归：4K@150% 下 DPI unaware 进程看到 2560×1440 虚拟桌面，命令行却传了物理 -xres 3840 -yres 2160。
        // 旧算法（按 -xres 居中）得 (-640,-360)，窗口被推出屏幕；新算法按窗口实际尺寸居中 → (0,0) 正好铺满。
        var current = new WindowInterop.Rect { Left = -640, Top = -360, Right = 1920, Bottom = 1080 };
        var monitor = new WindowInterop.Rect { Left = 0, Top = 0, Right = 2560, Bottom = 1440 };
        var adjustment = new Ra3WindowAdjustment(Fullscreen: true, 3840, 2160, null, null, AllowAlwaysOnTop: false);

        var (left, top) = Ra3GameWindow.ComputePosition(current, monitor, adjustment);

        Assert.Equal(0, left);
        Assert.Equal(0, top);
    }

    [Fact]
    public void ComputePosition_OversizedWindowOverflowsSymmetrically()
    {
        // 窗口比显示器还大（如用户强传超大 -xres 而游戏没 clamp）：对称越界是最优放置，不再整体偏出屏幕。
        var current = new WindowInterop.Rect { Left = 0, Top = 0, Right = 3840, Bottom = 2160 };
        var monitor = new WindowInterop.Rect { Left = 0, Top = 0, Right = 2560, Bottom = 1440 };
        var adjustment = new Ra3WindowAdjustment(Fullscreen: true, 3840, 2160, null, null, AllowAlwaysOnTop: false);

        var (left, top) = Ra3GameWindow.ComputePosition(current, monitor, adjustment);

        Assert.Equal((2560 - 3840) / 2, left);
        Assert.Equal((1440 - 2160) / 2, top);
    }

    [Fact]
    public void ComputePosition_CentersWithinMonitorOrigin_NoSeamOnNegativeOriginMonitor()
    {
        // 负原点的左侧副显示器（Left=-1920）：居中基准必须是显示器本身而非桌面并集，
        // 否则 1920 宽的无边框窗口会横跨两个显示器留下"缝"；按显示器居中应正好铺满它（Left=-1920）。
        var current = new WindowInterop.Rect { Left = -600, Top = 0, Right = 1320, Bottom = 1080 };
        var monitor = new WindowInterop.Rect { Left = -1920, Top = 0, Right = 0, Bottom = 1080 };
        var adjustment = new Ra3WindowAdjustment(Fullscreen: true, 1920, 1080, null, null, AllowAlwaysOnTop: false);

        var (left, top) = Ra3GameWindow.ComputePosition(current, monitor, adjustment);

        Assert.Equal(-1920, left);
        Assert.Equal(0, top);
    }

    [Fact]
    public void ComputePosition_CentersSmallerWindowWithinMonitorOrigin()
    {
        var current = new WindowInterop.Rect { Left = 0, Top = 0, Right = 1600, Bottom = 900 };
        var monitor = new WindowInterop.Rect { Left = -1920, Top = 0, Right = 0, Bottom = 1080 };
        var adjustment = new Ra3WindowAdjustment(Fullscreen: true, 1600, 900, null, null, AllowAlwaysOnTop: false);

        var (left, top) = Ra3GameWindow.ComputePosition(current, monitor, adjustment);

        Assert.Equal(-1920 + (1920 - 1600) / 2, left);
        Assert.Equal(0 + (1080 - 900) / 2, top);
    }
}
