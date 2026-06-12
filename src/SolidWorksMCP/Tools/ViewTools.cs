using ModelContextProtocol.Server;
using System.ComponentModel;
using SolidWorksMCP.Core;
using SolidWorksMCP.Services;

namespace SolidWorksMCP.Tools;

[McpServerToolType]
public sealed class ViewTools(SolidWorksConnectionService sw)
{
    [McpServerTool, Description("Zoom the active view to fit the whole model.")]
    public string ZoomToFit() => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();
        doc.ViewZoomtofit2();
        return "Zoomed to fit.";
    });

    [McpServerTool, Description("Switch to a standard view orientation: front, back, left, right, top, bottom, isometric.")]
    public string SetStandardView(
        [Description("View name: front, back, left, right, top, bottom, isometric (or French equivalents)")] string viewName) => ToolRunner.Run(() =>
    {
        int? id = SwConstants.StandardViewId(viewName);
        if (id is null)
            return $"Unknown view '{viewName}'. Use front, back, left, right, top, bottom or isometric.";

        dynamic doc = sw.GetActiveDoc();
        doc.ShowNamedView2(string.Empty, id.Value);
        doc.ViewZoomtofit2();
        return $"View set to {viewName}.";
    });

    [McpServerTool, Description("Rebuild the active document (equivalent of Ctrl+B). Set force=true for a full forced rebuild (Ctrl+Q).")]
    public string Rebuild(
        [Description("Force a full rebuild of all features? Default false.")] bool force = false) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();
        bool ok = force ? doc.ForceRebuild3(false) : doc.EditRebuild3();
        return ok ? $"{(force ? "Forced rebuild" : "Rebuild")} completed." : "Rebuild reported errors — check the feature tree.";
    });
}
