using ModelContextProtocol.Server;
using System.ComponentModel;
using SolidWorksMCP.Core;
using SolidWorksMCP.Services;

namespace SolidWorksMCP.Tools;

/// <summary>
/// Solid feature creation. These call the long-signature FeatureManager
/// methods; arguments mirror the SolidWorks 2025 API documentation.
/// </summary>
[McpServerToolType]
public sealed class GeometryTools(SolidWorksConnectionService sw)
{
    [McpServerTool, Description("Extrude the active or last sketch into a boss/base feature (blind end condition). Depth in millimeters. The sketch must be closed.")]
    public string CreateExtrusion(
        [Description("Extrusion depth in mm")] double depthMm,
        [Description("Reverse the extrusion direction? Default false.")] bool reverseDirection = false,
        [Description("Draft angle in degrees. Default 0.")] double draftAngleDeg = 0) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();

        // Exit sketch mode if a sketch is being edited (the sketch stays selected)
        if (doc.SketchManager.ActiveSketch is not null)
            doc.SketchManager.InsertSketch(true);

        dynamic? feat = doc.FeatureManager.FeatureExtrusion3(
            true,                                   // sd: single direction
            false,                                  // flip side to cut
            reverseDirection,                       // dir
            SwConstants.EndCondBlind,               // t1: end condition dir 1
            SwConstants.EndCondBlind,               // t2: end condition dir 2
            UnitsHelper.MmToMeters(depthMm),        // d1: depth dir 1 (meters)
            0.0,                                    // d2: depth dir 2
            draftAngleDeg != 0,                     // dchk1: draft on?
            false,                                  // dchk2
            false,                                  // ddir1: draft outward
            false,                                  // ddir2
            UnitsHelper.DegreesToRadians(draftAngleDeg), // dang1 (radians)
            0.0,                                    // dang2
            false, false,                           // offset reverse 1/2
            false, false,                           // translate surface 1/2
            true,                                   // merge result
            true,                                   // use feature scope
            true,                                   // use auto select
            0,                                      // t0: start condition (sketch plane)
            0.0,                                    // start offset
            false);                                 // flip start offset
        return feat is null
            ? "Extrusion failed. Make sure a closed sketch is selected or active."
            : $"Extrusion '{feat.Name}' created ({depthMm} mm).";
    });

    [McpServerTool, Description("Cut-extrude the active or last sketch through the part. Depth in millimeters, or through-all.")]
    public string CreateCutExtrusion(
        [Description("Cut depth in mm (ignored when throughAll is true)")] double depthMm = 10,
        [Description("Cut through the entire body? Default false.")] bool throughAll = false,
        [Description("Reverse the cut direction? Default false.")] bool reverseDirection = false) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();

        if (doc.SketchManager.ActiveSketch is not null)
            doc.SketchManager.InsertSketch(true);

        int endCond = throughAll ? SwConstants.EndCondThroughAll : SwConstants.EndCondBlind;
        dynamic? feat = doc.FeatureManager.FeatureCut4(
            true,                                   // single direction
            false,                                  // flip side to cut
            reverseDirection,
            endCond, SwConstants.EndCondBlind,
            UnitsHelper.MmToMeters(depthMm), 0.0,
            false, false, false, false,
            0.0, 0.0,
            false, false, false, false,
            false,                                  // normal cut
            true, true, true, true,
            false,                                  // use feature scope
            0, 0.0, false,                          // start condition
            false);                                 // opt. assembly feature scope
        return feat is null
            ? "Cut failed. Make sure a closed sketch is selected or active."
            : $"Cut '{feat.Name}' created.";
    });

    [McpServerTool, Description("Revolve the active or last sketch around a selected axis/centerline into a solid. Angle in degrees (360 = full revolution).")]
    public string CreateRevolve(
        [Description("Revolution angle in degrees. Default 360.")] double angleDeg = 360) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();

        if (doc.SketchManager.ActiveSketch is not null)
            doc.SketchManager.InsertSketch(true);

        dynamic? feat = doc.FeatureManager.FeatureRevolve2(
            true,                                   // single direction
            true,                                   // is solid
            false,                                  // is thin
            false, false, false,                    // cut/reverse/both directions
            SwConstants.EndCondBlind, SwConstants.EndCondBlind,
            UnitsHelper.DegreesToRadians(angleDeg), 0.0,
            false, false,
            0.0, 0.0,
            0, 0.0, 0.0,
            true, true, true);
        return feat is null
            ? "Revolve failed. The sketch needs a centerline or a selected axis to revolve around."
            : $"Revolve '{feat.Name}' created ({angleDeg}°).";
    });

    [McpServerTool, Description("Add a constant-radius fillet to the currently selected edges or faces. Select edges first with SelectByName. Radius in millimeters.")]
    public string CreateFillet(
        [Description("Fillet radius in mm")] double radiusMm) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();

        // 195 = standard option mask used by SW macro recordings for simple fillets
        dynamic? feat = doc.FeatureManager.FeatureFillet3(
            195, UnitsHelper.MmToMeters(radiusMm), 0, 0, 0, 0, 0,
            null, null, null, null, null, null, null);
        return feat is null
            ? "Fillet failed. Select one or more edges or faces first (SelectByName with type EDGE/FACE)."
            : $"Fillet '{feat.Name}' created (r = {radiusMm} mm).";
    });

    [McpServerTool, Description("Add an equal-distance chamfer to the currently selected edges. Distance in millimeters.")]
    public string CreateChamfer(
        [Description("Chamfer distance in mm")] double distanceMm,
        [Description("Chamfer angle in degrees. Default 45.")] double angleDeg = 45) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();

        // InsertFeatureChamfer: type 1 = angle-distance
        dynamic? feat = doc.FeatureManager.InsertFeatureChamfer(
            4,                                      // swFeatureChamferOption: keep features
            1,                                      // swChamferType: angle-distance
            UnitsHelper.MmToMeters(distanceMm),
            UnitsHelper.DegreesToRadians(angleDeg),
            0, 0, 0, 0);
        return feat is null
            ? "Chamfer failed. Select one or more edges first (SelectByName with type EDGE)."
            : $"Chamfer '{feat.Name}' created ({distanceMm} mm, {angleDeg}°).";
    });
}
