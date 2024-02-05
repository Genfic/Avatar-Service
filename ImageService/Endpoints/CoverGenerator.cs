using ImageService.Infrastructure;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ImageService.Endpoints;

public static class CoverGenerator
{
    private const int BaseSize = 200;
    private const float BorderOffset = 10f;
    private const float BorderWidth = 4;
    
    public static async Task<byte[]> Generate(string name, string author, string ext, int width, int height)
    {
        var maxSize = width > height ? width : height;
        var innerWidth = width - BorderOffset * 2;

        // Generate colours
        var seed = name.GetDeterministicHashCode();
        var rng = new Random(seed);

        var hue = rng.Next(0, 360);
        var saturation = (float)(rng.NextDouble() * 0.8 + 0.1);
        var lightness = (float)(rng.NextDouble() * 0.8 + 0.1);
        
        var startColor = new Hsl(hue, saturation, lightness + 0.05f).GetRgbColor();
        var endColor = new Hsl(hue, saturation, lightness - 0.05f).GetRgbColor();
        var textColor = lightness <= 0.5 
            ? Color.FromRgba(255, 255, 255, 200) 
            : Color.FromRgba(0, 0, 0, 200);

        // Generate image
        using var image = new Image<Rgba32>(width, height, Color.White);
        image.Metadata.HorizontalResolution = width;
        image.Metadata.VerticalResolution = height;
        
        // Add gradient
        image.Mutate(i =>
        {
            var colorStops = new[]
            {
                new ColorStop(0f, startColor), 
                new ColorStop(1f, endColor)
            };
            i.Fill(new RadialGradientBrush(new PointF(width * 0.1f, height * 0.1f), maxSize, GradientRepetitionMode.None, colorStops));
        });
        
        // Create font
        FontCollection collection = new();
        var family = collection.Add("Fonts/Montserrat-Regular.ttf");
        var font = family.CreateFont(BaseSize, FontStyle.Regular);
        
        // Resize text
        var resizedTitleFont = ResizeFont(name, width, height, font);

        // Add text
        var titleTextOptions = new RichTextOptions(resizedTitleFont)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Origin = new PointF(width * 0.5f, BorderOffset * 2f),
            WrappingLength = innerWidth - BorderWidth * 2,
            TextAlignment = TextAlignment.Center
        };
        
        image.Mutate(i =>
        {
            i.DrawText(titleTextOptions, name, textColor);
        });

        var resizedAuthorFont = ResizeFont(name, width, height, font, .6f);

        var authorTextOptions = new RichTextOptions(resizedAuthorFont)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Origin = new PointF(width * .5f, height - (BorderOffset * 2f)),
            WrappingLength = innerWidth - BorderWidth * 2,
            TextAlignment = TextAlignment.Center
        };
        
        image.Mutate(i =>
        {
            i.DrawText(authorTextOptions, author, textColor);
        });
        
        // Create border
        image.Mutate(ctx =>
        {            
            var border = new RectangleF(BorderOffset, BorderOffset, innerWidth, height - BorderOffset * 2);
            var colorStops = new[]
            {
                new ColorStop(0f, endColor),
                new ColorStop(1f, startColor), 
            };
            ctx.Draw(textColor, BorderWidth + 0.5f, border);
            ctx.Draw(new RadialGradientBrush(new PointF(width * 0.1f, height * 0.1f), maxSize, GradientRepetitionMode.None, colorStops), BorderWidth, border);
        });
        
        // Encode image
        var ms = new MemoryStream();
        IImageEncoder encoder = ext.ToLower() switch
        {
            "webp"          => new WebpEncoder(),
            "jpg" or "jpeg" => new JpegEncoder(),
            _ /* PNG */     => new PngEncoder()
        };
        await image.SaveAsync(ms, encoder);
        ms.Seek(0, SeekOrigin.Begin);
        
        return ms.ToArray();
    }

    private static Font ResizeFont(string text, int width, int height, Font font, float scale = 1f)
    {
        var vPadding = height / 10f;
        var hPadding = width / 10f + BorderOffset / 2;
        
        var vector = TextBuilder.GenerateGlyphs(text, new TextOptions(font)
        {
            WrappingLength = width,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var vScale = 1 / (vector.Bounds.Height / (height - vPadding * 3));
        var hScale = 1 / (vector.Bounds.Width / (width - hPadding * 3));
        var size = Math.Min(BaseSize * Math.Min(vScale, hScale), height * .5f) * scale;

        return new Font(font, size);
    }
}

public static class GenerateCoverHelpers
{
    public static WebApplication MapGenerateCovers(this WebApplication app)
    {
        app
            .MapGet("cover/{title}.{ext}", async (string title, string author, string ext, int? width, int? height, HttpResponse res) =>
            {
                var imageStream = await CoverGenerator.Generate(
                    title, 
                    author,
                    ext, 
                    width ?? AvatarGenerator.BaseSize, 
                    height ?? (int)Math.Round((width ?? AvatarGenerator.BaseSize) * 1.25)
                );
                
                res.Headers.Append("Cache-Control", $"public, immutable, max-age={TimeSpan.FromDays(365).TotalSeconds}");
                return TypedResults.File(imageStream, $"image/{ext}");
            })
            .WithName("GenerateCover")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Title to appear on the cover";
                operation.Parameters[1].Description = "author of the book";
                operation.Parameters[2].Description = "Image format, one of [jpg, jpeg, png, webp]";
                operation.Parameters[3].Description = $"Image width in pixels, default {AvatarGenerator.BaseSize}";
                operation.Parameters[4].Description = $"Image height in pixels, default {(int)Math.Round(AvatarGenerator.BaseSize * 1.25)}";
                return operation;
            });
        return app;
    }
}