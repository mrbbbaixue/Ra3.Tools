using Ra3.Tools.Installation;
using Ra3.Tools.Interop.Windows;

namespace Ra3.Tools.Test.Installation;

public sealed class Ra3GameWindowTests
{
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
    public void ComputePosition_FullscreenWithoutResolutionKeepsCurrentPosition()
    {
        var current = new WindowInterop.Rect { Left = 42, Top = 24, Right = 1962, Bottom = 1104 };
        var desktop = new WindowInterop.Rect { Left = 0, Top = 0, Right = 2560, Bottom = 1440 };
        var adjustment = new Ra3WindowAdjustment(Fullscreen: true, null, null, null, null, AllowAlwaysOnTop: false);

        var (left, top) = Ra3GameWindow.ComputePosition(current, desktop, adjustment);

        Assert.Equal(42, left);
        Assert.Equal(24, top);
    }
}
