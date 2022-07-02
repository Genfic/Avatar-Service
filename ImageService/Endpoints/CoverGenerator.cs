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
    
    public static async Task<byte[]> Generate(string name, string ext, int width, int height)
    {
        var maxSize = width > height ? width : height;
        var vPadding = height / 10f;
        var hPadding = width / 10f + BorderOffset / 2;
        var initials = Initials(name);
        
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
        var vector = TextBuilder.GenerateGlyphs(initials, new TextOptions(font));

        var vScale = 1 / (vector.Bounds.Height / (height - vPadding * 2));
        var hScale = 1 / (vector.Bounds.Width / (width - hPadding * 2));
        var size = Math.Min(BaseSize * Math.Min(vScale, hScale), height * .5f);
        
        var resizedFont = new Font(font, size);
        
        // Add text
        var options = new TextOptions(resizedFont)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Origin = new PointF(width * 0.5f, height * 0.5f)
        };
        
        image.Mutate(i =>
        {
            i.DrawText(options, initials, textColor);
        });
        
        // Create border
        image.Mutate(ctx =>
        {            
            var border = new RectangleF(BorderOffset, BorderOffset, width - BorderOffset * 2, height - BorderOffset * 2);
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
    
    private static string Initials(string name) => string.Join(string.Empty, name
        .Split(' ', '_', '-')
        .Select(s => s.Trim().ToUpper()[0]));
}

public static class GenerateCoverHelpers
{
    public static WebApplication MapGenerateCovers(this WebApplication app)
    {
        app
            .MapGet("cover/{name}.{ext}", async (string name, string ext, int? width, int? height, HttpResponse res) =>
            {
                var imageStream = await CoverGenerator.Generate(
                    name, 
                    ext, 
                    width ?? AvatarGenerator.BaseSize, 
                    height ?? (int)Math.Round(AvatarGenerator.BaseSize * 1.25)
                );
                
                res.Headers.Add("Cache-Control", $"public, immutable, max-age={TimeSpan.FromDays(365).TotalSeconds}");
                return Results.File(imageStream, $"image/{ext}");
            })
            .WithName("GenerateCover");
        return app;
    }
}