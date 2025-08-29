using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ImageSharpImage = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;
using SystemBitmap = System.Drawing.Bitmap;

namespace ECG_API.Infrastructure.Extensions
{
    public static class ImageConversionExtensions
    {

        public static SystemBitmap ToBitmap(this ImageSharpImage image)
        {
            using var memoryStream = new MemoryStream();
            image.SaveAsBmp(memoryStream);
            memoryStream.Position = 0;
            return new SystemBitmap(memoryStream);
        }
        
        public static ImageSharpImage ToImageSharp(this SystemBitmap bitmap)
        {
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            memoryStream.Position = 0;
            return ImageSharpImage.Load<Rgba32>(memoryStream);
        }
    }
}