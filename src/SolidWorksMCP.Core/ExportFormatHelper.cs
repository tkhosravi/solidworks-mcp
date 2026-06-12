namespace SolidWorksMCP.Core;

/// <summary>
/// Validates export formats per document type. SolidWorks exports are driven
/// by the target file extension passed to IModelDocExtension::SaveAs3, so the
/// only real work is knowing which extensions are legal for which documents.
/// </summary>
public static class ExportFormatHelper
{
    private sealed record Format(string Extension, int[] AllowedDocTypes);

    private static readonly Dictionary<string, Format> Formats = new(StringComparer.OrdinalIgnoreCase)
    {
        // 3D formats — parts & assemblies
        ["step"] = new(".step", [SwConstants.DocPart, SwConstants.DocAssembly]),
        ["stp"] = new(".step", [SwConstants.DocPart, SwConstants.DocAssembly]),
        ["iges"] = new(".igs", [SwConstants.DocPart, SwConstants.DocAssembly]),
        ["igs"] = new(".igs", [SwConstants.DocPart, SwConstants.DocAssembly]),
        ["stl"] = new(".stl", [SwConstants.DocPart, SwConstants.DocAssembly]),
        ["3mf"] = new(".3mf", [SwConstants.DocPart, SwConstants.DocAssembly]),
        ["parasolid"] = new(".x_t", [SwConstants.DocPart, SwConstants.DocAssembly]),
        ["x_t"] = new(".x_t", [SwConstants.DocPart, SwConstants.DocAssembly]),
        ["sat"] = new(".sat", [SwConstants.DocPart, SwConstants.DocAssembly]),
        ["obj"] = new(".obj", [SwConstants.DocPart, SwConstants.DocAssembly]),
        ["vrml"] = new(".wrl", [SwConstants.DocPart, SwConstants.DocAssembly]),

        // 2D formats — drawings (dxf/dwg also work on sheet metal parts)
        ["dxf"] = new(".dxf", [SwConstants.DocDrawing, SwConstants.DocPart]),
        ["dwg"] = new(".dwg", [SwConstants.DocDrawing, SwConstants.DocPart]),

        // Documents / images — everything
        ["pdf"] = new(".pdf", [SwConstants.DocPart, SwConstants.DocAssembly, SwConstants.DocDrawing]),
        ["png"] = new(".png", [SwConstants.DocPart, SwConstants.DocAssembly, SwConstants.DocDrawing]),
        ["jpg"] = new(".jpg", [SwConstants.DocPart, SwConstants.DocAssembly, SwConstants.DocDrawing]),
        ["tif"] = new(".tif", [SwConstants.DocPart, SwConstants.DocAssembly, SwConstants.DocDrawing]),
        ["edrawings"] = new(".eprt", [SwConstants.DocPart, SwConstants.DocAssembly, SwConstants.DocDrawing]),
    };

    public static IReadOnlyCollection<string> SupportedFormats =>
        Formats.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();

    public static bool IsSupported(string format) => Formats.ContainsKey(format.Trim().TrimStart('.'));

    /// <summary>
    /// Resolves the output path for an export. Throws ArgumentException for an
    /// unknown format or a format/document-type mismatch.
    /// </summary>
    public static string ResolveOutputPath(string sourceModelPath, string format, int swDocType, string? outputDirectory = null)
    {
        var key = format.Trim().TrimStart('.');
        if (!Formats.TryGetValue(key, out var fmt))
            throw new ArgumentException(
                $"Unsupported export format '{format}'. Supported: {string.Join(", ", SupportedFormats)}");

        if (!fmt.AllowedDocTypes.Contains(swDocType))
            throw new ArgumentException(
                $"Format '{key}' cannot be exported from a {SwConstants.DocTypeName(swDocType)} document.");

        if (string.IsNullOrWhiteSpace(sourceModelPath))
            throw new ArgumentException("The document must be saved at least once before exporting.");

        // Model paths come from SolidWorks and are always Windows paths; parse
        // them by hand so behaviour does not depend on the host OS (tests run
        // on macOS/Linux too).
        var (dir, fileName) = SplitWindowsPath(sourceModelPath);
        var dot = fileName.LastIndexOf('.');
        var stem = dot > 0 ? fileName[..dot] : fileName;

        var targetDir = outputDirectory ?? dir;
        var separator = targetDir.Contains('\\') || targetDir.Contains(':') ? '\\' : '/';
        return $"{targetDir.TrimEnd('\\', '/')}{separator}{stem}{fmt.Extension}";
    }

    private static (string Directory, string FileName) SplitWindowsPath(string path)
    {
        var idx = path.LastIndexOfAny(['\\', '/']);
        return idx < 0 ? ("", path) : (path[..idx], path[(idx + 1)..]);
    }
}
