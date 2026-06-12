using SolidWorksMCP.Core;
using Xunit;

namespace SolidWorksMCP.Core.Tests;

public class UnitsHelperTests
{
    [Theory]
    [InlineData(1000, 1)]
    [InlineData(25.4, 0.0254)]
    [InlineData(0, 0)]
    [InlineData(-50, -0.05)]
    public void MmToMeters_Converts(double mm, double expectedMeters)
    {
        Assert.Equal(expectedMeters, UnitsHelper.MmToMeters(mm), precision: 10);
    }

    [Fact]
    public void MmToMeters_And_MetersToMm_AreInverse()
    {
        Assert.Equal(42.5, UnitsHelper.MetersToMm(UnitsHelper.MmToMeters(42.5)), precision: 10);
    }

    [Theory]
    [InlineData(180, Math.PI)]
    [InlineData(90, Math.PI / 2)]
    [InlineData(0, 0)]
    [InlineData(360, 2 * Math.PI)]
    public void DegreesToRadians_Converts(double degrees, double expectedRadians)
    {
        Assert.Equal(expectedRadians, UnitsHelper.DegreesToRadians(degrees), precision: 10);
    }

    [Fact]
    public void DegreesAndRadians_AreInverse()
    {
        Assert.Equal(45, UnitsHelper.RadiansToDegrees(UnitsHelper.DegreesToRadians(45)), precision: 10);
    }
}
