using Ra3.Tools.Graphics;

namespace Ra3.Tools.Test.Graphics;

public sealed class Ra3DisplayAdvisorTests
{
    [Fact]
    public void GetRecommendedDisplayMode_PrioritizesUltrawideProfile()
    {
        var supported = new[]
        {
            new DisplayMode(2560, 1080, 60, 32),
            new DisplayMode(3440, 1440, 60, 32),
            new DisplayMode(3840, 1600, 60, 32),
            new DisplayMode(2560, 1440, 60, 32)
        };

        var recommended = Ra3DisplayAdvisor.GetRecommendedDisplayMode(supported, new DisplayMode(3440, 1440, 144, 32));

        Assert.Equal(new DisplayMode(3840, 1600, 60, 32), recommended);
    }

    [Fact]
    public void GetRecommendedDisplayMode_DoesNotRecommend800x600()
    {
        var supported = new[]
        {
            new DisplayMode(800, 600, 60, 32),
            new DisplayMode(1024, 768, 60, 32),
            new DisplayMode(1600, 1200, 60, 32)
        };

        var recommended = Ra3DisplayAdvisor.GetRecommendedDisplayMode(supported, new DisplayMode(1024, 768, 60, 32));

        Assert.NotEqual(new DisplayMode(800, 600, 60, 32), recommended);
    }
}
