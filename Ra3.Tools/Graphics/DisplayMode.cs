namespace Ra3.Tools.Graphics;

public readonly record struct DisplayMode(int Width, int Height, int RefreshRate = 60, int BitsPerPixel = 32)
{
    public override string ToString()
    {
        return $"{Width}x{Height} @ {RefreshRate}Hz ({BitsPerPixel}bpp)";
    }
}
