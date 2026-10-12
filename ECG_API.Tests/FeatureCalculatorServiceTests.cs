using ECG_API.Infrastructure.ImageProcessing;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
namespace ECG_API.Tests;

public class FeatureCalculatorServiceTests
{
    private static FeatureCalculatorService CreateService()
    {
        return new FeatureCalculatorService(NullLogger<FeatureCalculatorService>.Instance);
    }
    private const int ExpectedLength = 40;
    [Fact]
    public void CalculateFeatures_TooNarrowStrip_ReturnsZeros()
    {
        int testBaseLineY = 10;
        using var testImage = new Image<Rgba32>(19, 1080);
        FeatureCalculatorService testCalculation = CreateService();
        float[] result = testCalculation.CalculateFeatures(testImage, testBaseLineY);
        Assert.Equal(ExpectedLength,result.Length);
        Assert.All(result, x => Assert.Equal(0f, x));
    }

    [Fact]
    public void CalculateFeatures_NegativeBaseline_ReturnsZeros()
    {
        int testBaseLineY = -1;
        using var testImage = new Image<Rgba32>(120, 1080);
        FeatureCalculatorService testCalculation = CreateService();
        float[] result = testCalculation.CalculateFeatures(testImage, testBaseLineY);
        Assert.Equal(ExpectedLength,result.Length);
        Assert.All(result, x => Assert.Equal(0f, x));
    }

    [Fact]
    public void CalculateFeatures_LineAboveBaseline_ReturnsKnownAreas()
    {
        var white = new Rgba32(255, 255, 255, 255);
        var black = new Rgba32(0, 0, 0, 255);
        using var testImage = new Image<Rgba32>(200, 100, white);
        for (int x = 0; x < testImage.Width; x++)
        {
            testImage[x, 30] = black;
        }
        int testBaseLineY = 50;
        FeatureCalculatorService testCalculation = CreateService();
        float[] result = testCalculation.CalculateFeatures(testImage, testBaseLineY);
        Assert.Equal(ExpectedLength,result.Length);
        for (int i = 0; i < result.Length; i += 2)
        {
            Assert.Equal(200, result[i]);
            Assert.Equal(0, result[i + 1]);
        }
    }
}
