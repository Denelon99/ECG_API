using ECG_API.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ECG_API.Infrastructure.ImageProcessing;
 
public class EcgBaselineService(ILogger<EcgBaselineService> logger) : IEcgBaselineService
{
    public int FindBaseline(Image<Rgba32> ecgStrip)
    {
        if (ecgStrip == null || ecgStrip.Width == 0 || ecgStrip.Height == 0)
        {
            return -1;
        }

        var horizontalProjection = new int[ecgStrip.Height];

        ecgStrip.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                int blackPixelCount = 0;
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    if (row[x].R == 0 && row[x].G == 0 && row[x].B == 0)
                    {
                        blackPixelCount++;
                    }
                }
                horizontalProjection[y] = blackPixelCount;
            }
        });

        int maxCount = 0;
        int baselineY = -1;
        for (int y = 0; y < horizontalProjection.Length; y++)
        {
            if (horizontalProjection[y] > maxCount)
            {
                maxCount = horizontalProjection[y];
                baselineY = y;
            }
        }
        return baselineY;
    }
}