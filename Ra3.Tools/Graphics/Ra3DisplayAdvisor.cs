namespace Ra3.Tools.Graphics;

/// <summary>根据显示器纵横比与 RA3 偏好分辨率，从支持的模式中推荐最佳分辨率。</summary>
public static class Ra3DisplayAdvisor
{
    private sealed record ResolutionProfile(string Name, double AspectRatio, (int Width, int Height)[] PreferredModes);

    private static readonly ResolutionProfile[] Profiles =
    [
        new("21:9", 21d / 9d, [(3840, 1600), (3440, 1440), (2560, 1080)]),
        new("16:9", 16d / 9d, [(3840, 2160), (2560, 1440), (1920, 1080), (1280, 720)]),
        new("4:3", 4d / 3d, [(1600, 1200), (1400, 1050), (1280, 960), (1024, 768)])
    ];

    private static readonly ResolutionProfile DefaultProfile = Profiles[1];

    /// <summary>返回推荐显示模式；无可用模式时返回 null。优先匹配当前纵横比的首选分辨率，其次 16:9。</summary>
    /// <param name="supported">显示器支持的模式集合。</param>
    /// <param name="current">当前显示模式（可为 null）。</param>
    public static DisplayMode? GetRecommendedDisplayMode(IReadOnlyCollection<DisplayMode> supported, DisplayMode? current)
    {
        if (supported.Count == 0)
        {
            return null;
        }

        var currentProfile = current is { } currentMode ? FindProfile(currentMode) : null;
        if (currentProfile != null)
        {
            var profileMode = FindModeForProfile(supported, currentProfile);
            if (profileMode != null)
            {
                return profileMode;
            }
        }

        if (currentProfile != DefaultProfile)
        {
            var defaultMode = FindModeForProfile(supported, DefaultProfile);
            if (defaultMode != null)
            {
                return defaultMode;
            }
        }

        if (current is { } actualCurrent && supported.Any(mode => mode.Width == actualCurrent.Width && mode.Height == actualCurrent.Height))
        {
            return supported.Where(mode => mode.Width == actualCurrent.Width && mode.Height == actualCurrent.Height)
                .OrderByDescending(mode => mode.RefreshRate)
                .First();
        }

        return supported.OrderByDescending(mode => mode.Width * mode.Height)
            .ThenByDescending(mode => mode.RefreshRate)
            .First();
    }

    private static ResolutionProfile? FindProfile(DisplayMode mode)
    {
        return Profiles.FirstOrDefault(profile => IsApproximatelyAspectRatio(mode, profile.AspectRatio));
    }

    private static DisplayMode? FindModeForProfile(IEnumerable<DisplayMode> supported, ResolutionProfile profile)
    {
        foreach (var (width, height) in profile.PreferredModes)
        {
            var match = supported.Where(mode => mode.Width == width && mode.Height == height)
                .OrderByDescending(mode => mode.RefreshRate)
                .FirstOrDefault();
            if (match != default)
            {
                return match;
            }
        }

        return supported.Where(mode => IsApproximatelyAspectRatio(mode, profile.AspectRatio))
            .OrderByDescending(mode => mode.Width * mode.Height)
            .ThenByDescending(mode => mode.RefreshRate)
            .FirstOrDefault();
    }

    private static bool IsApproximatelyAspectRatio(DisplayMode mode, double expectedAspectRatio)
    {
        var actualAspectRatio = (double)mode.Width / mode.Height;
        var relativeDifference = Math.Abs(actualAspectRatio - expectedAspectRatio) / expectedAspectRatio;
        return relativeDifference <= 0.03;
    }
}
