using OpenCvSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ImageSharpImage = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;

namespace ECG_API.Infrastructure.Extensions
{
    // Конвертація ImageSharp <-> OpenCV Mat через PNG-байти, без System.Drawing (працює лише на Windows).
    public static class ImageConversionExtensions
    {
        // Повертає 3-канальний BGR Mat (CV_8UC3) — сервіси очікують саме BGR для BGR2GRAY.
        public static Mat ToMat(this ImageSharpImage image)
        {
            using var memoryStream = new MemoryStream();
            image.SaveAsPng(memoryStream);
            return Cv2.ImDecode(memoryStream.ToArray(), ImreadModes.Color);
        }

        public static ImageSharpImage ToImageSharp(this Mat mat)
        {
            Cv2.ImEncode(".png", mat, out var pngBytes);
            return Image.Load<Rgba32>(pngBytes);
        }
    }
}
