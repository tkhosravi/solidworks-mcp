using SolidWorksMCP.Core;
using Xunit;

namespace SolidWorksMCP.Core.Tests;

public class ProgIdHelperTests
{
    [Theory]
    [InlineData(2024, 32)]
    [InlineData(2025, 33)]
    [InlineData(2026, 34)]
    public void VersionSuffix_FollowsYearMinus1992Rule(int year, int expected)
    {
        Assert.Equal(expected, ProgIdHelper.VersionSuffix(year));
    }

    [Fact]
    public void VersionedProgId_For2025_IsSldWorksApplication33()
    {
        Assert.Equal("SldWorks.Application.33", ProgIdHelper.VersionedProgId(2025));
    }

    [Fact]
    public void Candidates_TriesVersionedThenGeneric()
    {
        var candidates = ProgIdHelper.Candidates(2025);

        Assert.Equal(2, candidates.Count);
        Assert.Equal("SldWorks.Application.33", candidates[0]);
        Assert.Equal("SldWorks.Application", candidates[1]);
    }

    [Fact]
    public void VersionSuffix_RejectsPre1995Years()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProgIdHelper.VersionSuffix(1990));
    }
}
