using ModelContextProtocol.Server;
using System.ComponentModel;
using SolidWorksMCP.Core;
using SolidWorksMCP.Services;

namespace SolidWorksMCP.Tools;

/// <summary>
/// Sketch creation and 2D geometry. All coordinates are taken in millimeters
/// and converted to the meters the SolidWorks API expects.
/// </summary>
[McpServerToolType]
public sealed class SketchTools(SolidWorksConnectionService sw)
{
    [McpServerTool, Description("Insert a new sketch on a named plane or flat face in the active part (e.g. 'Front Plane', 'Top Plane', 'Right Plane', or localized names like 'Plan de face').")]
    public string InsertSketchOnPlane(
        [Description("Name of the plane or flat face")] string planeName) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();

        bool selected = doc.Extension.SelectByID2(planeName, "PLANE", 0, 0, 0, false, 0, null, 0);
        if (!selected)
            selected = doc.Extension.SelectByID2(planeName, "FACE", 0, 0, 0, false, 0, null, 0);

        if (!selected)
            return $"Could not select '{planeName}'. Make sure it's a valid plane or flat face name.";

        doc.SketchManager.InsertSketch(true);
        return $"Sketch opened on '{planeName}'. Use the sketch geometry tools to draw, then ExitSketch.";
    });

    [McpServerTool, Description("Exit (confirm) the currently active sketch and rebuild.")]
    public string ExitSketch() => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();
        doc.SketchManager.InsertSketch(true); // toggles out of sketch mode, keeping changes
        return "Sketch confirmed and exited.";
    });

    [McpServerTool, Description("Draw a line in the active sketch. Coordinates in millimeters, sketch plane 2D coordinates (z usually 0).")]
    public string SketchLine(
        [Description("Start X in mm")] double x1, [Description("Start Y in mm")] double y1,
        [Description("End X in mm")] double x2, [Description("End Y in mm")] double y2) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();
        RequireActiveSketch(doc);
        dynamic? seg = doc.SketchManager.CreateLine(
            UnitsHelper.MmToMeters(x1), UnitsHelper.MmToMeters(y1), 0,
            UnitsHelper.MmToMeters(x2), UnitsHelper.MmToMeters(y2), 0);
        return seg is null ? "Failed to create line." : $"Line created from ({x1}, {y1}) to ({x2}, {y2}) mm.";
    });

    [McpServerTool, Description("Draw a circle in the active sketch. Center and radius in millimeters.")]
    public string SketchCircle(
        [Description("Center X in mm")] double centerX,
        [Description("Center Y in mm")] double centerY,
        [Description("Radius in mm")] double radius) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();
        RequireActiveSketch(doc);
        dynamic? seg = doc.SketchManager.CreateCircleByRadius(
            UnitsHelper.MmToMeters(centerX), UnitsHelper.MmToMeters(centerY), 0,
            UnitsHelper.MmToMeters(radius));
        return seg is null ? "Failed to create circle." : $"Circle created at ({centerX}, {centerY}) r={radius} mm.";
    });

    [McpServerTool, Description("Draw a corner rectangle in the active sketch. Opposite corners in millimeters.")]
    public string SketchRectangle(
        [Description("First corner X in mm")] double x1, [Description("First corner Y in mm")] double y1,
        [Description("Opposite corner X in mm")] double x2, [Description("Opposite corner Y in mm")] double y2) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();
        RequireActiveSketch(doc);
        dynamic? segs = doc.SketchManager.CreateCornerRectangle(
            UnitsHelper.MmToMeters(x1), UnitsHelper.MmToMeters(y1), 0,
            UnitsHelper.MmToMeters(x2), UnitsHelper.MmToMeters(y2), 0);
        return segs is null ? "Failed to create rectangle." : $"Rectangle created from ({x1}, {y1}) to ({x2}, {y2}) mm.";
    });

    [McpServerTool, Description("Draw a center-point arc in the active sketch. Coordinates in millimeters.")]
    public string SketchArc(
        [Description("Center X in mm")] double centerX, [Description("Center Y in mm")] double centerY,
        [Description("Arc start X in mm")] double startX, [Description("Arc start Y in mm")] double startY,
        [Description("Arc end X in mm")] double endX, [Description("Arc end Y in mm")] double endY,
        [Description("Direction: 1 = counter-clockwise, -1 = clockwise")] int direction = 1) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();
        RequireActiveSketch(doc);
        dynamic? seg = doc.SketchManager.CreateArc(
            UnitsHelper.MmToMeters(centerX), UnitsHelper.MmToMeters(centerY), 0,
            UnitsHelper.MmToMeters(startX), UnitsHelper.MmToMeters(startY), 0,
            UnitsHelper.MmToMeters(endX), UnitsHelper.MmToMeters(endY), 0,
            (short)direction);
        return seg is null ? "Failed to create arc." : "Arc created.";
    });

    private static void RequireActiveSketch(dynamic doc)
    {
        if (doc.SketchManager.ActiveSketch is null)
            throw new InvalidOperationException(
                "No active sketch. Call InsertSketchOnPlane first (e.g. on 'Front Plane').");
    }
}
