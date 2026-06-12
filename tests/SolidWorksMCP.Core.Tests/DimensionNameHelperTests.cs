using SolidWorksMCP.Core;
using Xunit;

namespace SolidWorksMCP.Core.Tests;

public class DimensionNameHelperTests
{
    [Theory]
    [InlineData("D1@Sketch1", "D1@Sketch1")]
    [InlineData("D1@Sketch1@Part1.SLDPRT", "D1@Sketch1")]
    [InlineData("  D2@Boss-Extrude1  ", "D2@Boss-Extrude1")]
    public void Normalize_KeepsNameAtFeatureForm(string input, string expected)
    {
        Assert.Equal(expected, DimensionNameHelper.Normalize(input));
    }

    [Theory]
    [InlineData("D1")]
    [InlineData("")]
    [InlineData("@@")]
    public void Normalize_RejectsNamesWithoutFeaturePart(string input)
    {
        var ex = Assert.Throws<ArgumentException>(() => DimensionNameHelper.Normalize(input));
        Assert.Contains("D1@Sketch1", ex.Message); // error message shows the expected form
    }

    [Theory]
    [InlineData("A1@Sketch1", true)]
    [InlineData("D1@Sketch1", false)]
    [InlineData("angle@Sketch1", true)]
    public void LooksAngular_UsesLeadingLetterHeuristic(string name, bool expected)
    {
        Assert.Equal(expected, DimensionNameHelper.LooksAngular(name));
    }
}
