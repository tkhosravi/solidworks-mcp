using ModelContextProtocol.Server;
using System.ComponentModel;
using SolidWorksMCP.Core;
using SolidWorksMCP.Services;

namespace SolidWorksMCP.Tools;

[McpServerToolType]
public sealed class ExportTools(SolidWorksConnectionService sw)
{
    [McpServerTool, Description("Export the active document to another format (step, iges, stl, 3mf, parasolid, sat, obj, vrml, pdf, dxf, dwg, png, jpg, tif, edrawings). Returns the path of the exported file.")]
    public string ExportActiveDocument(
        [Description("Target format, e.g. 'step', 'stl', 'pdf'")] string format,
        [Description("Optional output directory. Defaults to the model's folder.")] string? outputDirectory = null) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();
        int docType = doc.GetType();
        string modelPath = doc.GetPathName();

        string outPath = ExportFormatHelper.ResolveOutputPath(modelPath, format, docType, outputDirectory);

        int err = 0, warn = 0;
        bool ok = doc.Extension.SaveAs3(
            outPath,
            0,                                  // swSaveAsCurrentVersion
            SwConstants.SaveAsOptionsSilent,
            null,                               // export data (defaults)
            null,                               // advanced save options
            ref err, ref warn);

        return ok
            ? $"Exported to: {outPath}"
            : $"Export failed (error {err}, warning {warn}). Target: {outPath}";
    });

    [McpServerTool, Description("List all export formats supported by the ExportActiveDocument tool.")]
    public string ListExportFormats() =>
        "Supported formats: " + string.Join(", ", ExportFormatHelper.SupportedFormats);

    [McpServerTool, Description("Capture a screenshot of the active document view to a PNG file.")]
    public string CaptureScreenshot(
        [Description("Absolute path of the PNG file to write, e.g. C:\\temp\\view.png")] string outputPath) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        // SaveBMP renders the current view; SW infers the image type from the extension
        bool ok = doc.SaveBMP(outputPath, 0, 0); // 0,0 = current window size
        return ok ? $"Screenshot saved: {outputPath}" : "Screenshot failed.";
    });
}
