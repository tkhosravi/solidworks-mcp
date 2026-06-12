using ModelContextProtocol.Server;
using System.ComponentModel;
using SolidWorksMCP.Core;
using SolidWorksMCP.Core.Models;
using SolidWorksMCP.Services;

namespace SolidWorksMCP.Tools;

[McpServerToolType]
public sealed class DrawingTools(SolidWorksConnectionService sw)
{
    [McpServerTool, Description("List the sheets of the active drawing, with their views.")]
    public string ListSheets() => ToolRunner.Run(() =>
    {
        dynamic doc = RequireDrawing();
        string activeSheet = doc.GetCurrentSheet().GetName();

        object[] names = (object[])doc.GetSheetNames();
        var sheets = new List<SheetInfo>();
        foreach (string name in names.Cast<string>())
        {
            var viewNames = new List<string>();
            // GetViews returns one array per sheet: [sheetViews][views]
            object? viewsBySheet = doc.GetViews();
            if (viewsBySheet is object[] sheetArrays)
            {
                foreach (object[] sheetViews in sheetArrays.Cast<object[]>())
                {
                    // First view of each array is the sheet itself
                    if (sheetViews.Length > 0 && ((dynamic)sheetViews[0]).Name == name)
                    {
                        foreach (dynamic v in sheetViews.Skip(1))
                            viewNames.Add((string)v.Name);
                    }
                }
            }

            sheets.Add(new SheetInfo(
                Name: name,
                IsActive: string.Equals(name, activeSheet, StringComparison.Ordinal),
                ViewNames: viewNames.ToArray()
            ));
        }
        return ToolRunner.ToJson(sheets);
    });

    [McpServerTool, Description("Activate a sheet of the active drawing by name.")]
    public string ActivateSheet(
        [Description("Sheet name")] string name) => ToolRunner.Run(() =>
    {
        dynamic doc = RequireDrawing();
        bool ok = doc.ActivateSheet(name);
        return ok ? $"Sheet '{name}' activated." : $"Sheet '{name}' not found.";
    });

    [McpServerTool, Description("Create standard views (front, top, right + isometric) of a model on the active drawing sheet.")]
    public string CreateStandardViews(
        [Description("Absolute path of the part or assembly to place on the drawing")] string modelPath) => ToolRunner.Run(() =>
    {
        if (!File.Exists(modelPath))
            return $"File not found: {modelPath}";

        dynamic doc = RequireDrawing();
        bool ok = doc.GenerateViewPaletteViews(modelPath);
        if (!ok)
            return "Could not generate the view palette for this model.";

        // Drop the standard 3-view layout
        bool created = doc.Create3rdAngleViews2(modelPath);
        return created
            ? "Standard third-angle views created. Use DropDrawingViewFromPalette for other orientations."
            : "Failed to create standard views.";
    });

    private dynamic RequireDrawing()
    {
        dynamic doc = sw.GetActiveDoc();
        if ((int)doc.GetType() != SwConstants.DocDrawing)
            throw new InvalidOperationException("The active document is not a drawing.");
        return doc;
    }
}
