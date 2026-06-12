using SolidWorksMCP.Core;
using Xunit;

namespace SolidWorksMCP.Core.Tests;

public class ExportFormatHelperTests
{
    private const string PartPath = @"C:\models\bracket.SLDPRT";

    [Theory]
    [InlineData("step", ".step")]
    [InlineData("STEP", ".step")]
    [InlineData(".stp", ".step")]
    [InlineData("stl", ".stl")]
    [InlineData("parasolid", ".x_t")]
    [InlineData("pdf", ".pdf")]
    public void ResolveOutputPath_MapsFormatToExtension(string format, string expectedExtension)
    {
        var result = ExportFormatHelper.ResolveOutputPath(PartPath, format, SwConstants.DocPart);

        Assert.EndsWith("bracket" + expectedExtension, result);
    }

    [Fact]
    public void ResolveOutputPath_KeepsModelDirectoryByDefault()
    {
        var result = ExportFormatHelper.ResolveOutputPath(PartPath, "step", SwConstants.DocPart);

        Assert.Equal(@"C:\models\bracket.step", result);
    }

    [Fact]
    public void ResolveOutputPath_UsesExplicitOutputDirectory()
    {
        var result = ExportFormatHelper.ResolveOutputPath(PartPath, "stl", SwConstants.DocPart, @"D:\exports");

        Assert.Equal(@"D:\exports\bracket.stl", result);
    }

    [Fact]
    public void ResolveOutputPath_RejectsUnknownFormat()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ExportFormatHelper.ResolveOutputPath(PartPath, "docx", SwConstants.DocPart));

        Assert.Contains("Unsupported export format", ex.Message);
    }

    [Fact]
    public void ResolveOutputPath_RejectsStepExportFromDrawing()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ExportFormatHelper.ResolveOutputPath(@"C:\models\bracket.SLDDRW", "step", SwConstants.DocDrawing));

        Assert.Contains("Drawing", ex.Message);
    }

    [Fact]
    public void ResolveOutputPath_AllowsDxfFromDrawingAndPart_ButNotAssembly()
    {
        // Drawings and sheet-metal parts can export DXF; assemblies cannot
        _ = ExportFormatHelper.ResolveOutputPath(@"C:\m\a.SLDDRW", "dxf", SwConstants.DocDrawing);
        _ = ExportFormatHelper.ResolveOutputPath(@"C:\m\a.SLDPRT", "dxf", SwConstants.DocPart);

        Assert.Throws<ArgumentException>(() =>
            ExportFormatHelper.ResolveOutputPath(@"C:\m\a.SLDASM", "dxf", SwConstants.DocAssembly));
    }

    [Fact]
    public void ResolveOutputPath_RejectsUnsavedDocument()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ExportFormatHelper.ResolveOutputPath("", "step", SwConstants.DocPart));

        Assert.Contains("saved at least once", ex.Message);
    }

    [Fact]
    public void IsSupported_AcceptsDotPrefixAndMixedCase()
    {
        Assert.True(ExportFormatHelper.IsSupported(".STEP"));
        Assert.True(ExportFormatHelper.IsSupported("Stl"));
        Assert.False(ExportFormatHelper.IsSupported("xyz"));
    }
}
