using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using SixLabors.Primitives;

namespace WeatherBeer.Models
{
    // Copyright: https://github.com/wearetriple/InHolland-CCD/blob/master/DemoQ/ImageHelper.cs
    public class ImageHelper
    {
        public static Stream AddTextToImage(Stream imageStream, params (string text, (float x, float y) position)[] texts)
        {

            var memoryStream = new MemoryStream();

            var image = Image.Load(imageStream);

            image
                .Clone(img =>
                {
                    foreach (var (text, (x, y)) in texts)
                    {
                        img.DrawText(text, SystemFonts.CreateFont("Verdana", 24), Rgba32.OrangeRed, new PointF(x, y));
                    }
                }).SaveAsPng(memoryStream);

            memoryStream.Position = 0;

            return memoryStream;
        }
    }
}
