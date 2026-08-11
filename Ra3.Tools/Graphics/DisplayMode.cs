namespace Ra3.Tools.Graphics;

/// <summary>一个显示模式：分辨率 + 刷新率 + 色深。</summary>
/// <param name="Width">宽度（像素）。</param>
/// <param name="Height">高度（像素）。</param>
/// <param name="RefreshRate">刷新率（Hz），默认 60。</param>
/// <param name="BitsPerPixel">色深（位），默认 32。</param>
public readonly record struct DisplayMode(int Width, int Height, int RefreshRate = 60, int BitsPerPixel = 32)
{
    /// <summary>返回 "宽x高 @ 刷新率Hz (色深bpp)" 形式。</summary>
    public override string ToString()
    {
        return $"{Width}x{Height} @ {RefreshRate}Hz ({BitsPerPixel}bpp)";
    }
}
