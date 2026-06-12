using SolidWorksMCP.Core;
using Xunit;

namespace SolidWorksMCP.Core.Tests;

public class SwConstantsTests
{
    [Theory]
    [InlineData(1, "Part")]
    [InlineData(2, "Assembly")]
    [InlineData(3, "Drawing")]
    [InlineData(99, "Unknown(99)")]
    public void DocTypeName_MapsSwDocumentTypes(int swType, string expected)
    {
        Assert.Equal(expected, SwConstants.DocTypeName(swType));
    }

    [Theory]
    [InlineData(0, "Suppressed")]
    [InlineData(1, "Lightweight")]
    [InlineData(2, "Resolved")]
    [InlineData(3, "Resolved")]
    [InlineData(42, "Unknown(42)")]
    public void ComponentStateName_MapsSuppressionStates(int state, string expected)
    {
        Assert.Equal(expected, SwConstants.ComponentStateName(state));
    }

    [Theory]
    [InlineData("front", SwConstants.ViewFront)]
    [InlineData("Face", SwConstants.ViewFront)]
    [InlineData("ISOMETRIC", SwConstants.ViewIsometric)]
    [InlineData("iso", SwConstants.ViewIsometric)]
    [InlineData("isométrique", SwConstants.ViewIsometric)]
    [InlineData("dessus", SwConstants.ViewTop)]
    [InlineData(" droite ", SwConstants.ViewRight)]
    public void StandardViewId_AcceptsEnglishAndFrenchNames(string name, int expectedId)
    {
        Assert.Equal(expectedId, SwConstants.StandardViewId(name));
    }

    [Fact]
    public void StandardViewId_ReturnsNullForUnknownView()
    {
        Assert.Null(SwConstants.StandardViewId("diagonal"));
    }
}
