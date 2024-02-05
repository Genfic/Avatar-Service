using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageService.Infrastructure;

public static class ColorHelpers
{
    public static Color GetRgbColor(this Hsl hsl)
    {
        return new Color((Rgba32)ColorSpaceConverter.ToRgb(hsl));
    }
}