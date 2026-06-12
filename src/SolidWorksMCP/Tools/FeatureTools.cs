using ModelContextProtocol.Server;
using System.ComponentModel;
using SolidWorksMCP.Core;
using SolidWorksMCP.Core.Models;
using SolidWorksMCP.Services;

namespace SolidWorksMCP.Tools;

[McpServerToolType]
public sealed class FeatureTools(SolidWorksConnectionService sw)
{
    [McpServerTool, Description("List all features in the active Part or Assembly document (the feature tree).")]
    public string ListFeatures() => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();

        var features = new List<FeatureInfo>();
        dynamic? feat = doc.FirstFeature();
        while (feat is not null)
        {
            features.Add(new FeatureInfo(
                Name: feat.Name,
                TypeName: feat.GetTypeName2(),
                IsSuppressed: feat.IsSuppressed2(SwConstants.ThisConfiguration, null)
            ));
            feat = feat.GetNextFeature();
        }

        return features.Count == 0 ? "No features found." : ToolRunner.ToJson(features);
    });

    [McpServerTool, Description("Suppress or unsuppress a named feature in the active document.")]
    public string SetFeatureSuppression(
        [Description("Exact name of the feature to modify")] string featureName,
        [Description("true to suppress, false to unsuppress")] bool suppress) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();

        dynamic? feat = doc.FeatureByName(featureName);
        if (feat is null)
            return $"Feature '{featureName}' not found.";

        int action = suppress ? SwConstants.SuppressFeature : SwConstants.UnsuppressFeature;
        bool ok = feat.SetSuppression2(action, SwConstants.ThisConfiguration, null);

        return ok
            ? $"Feature '{featureName}' is now {(suppress ? "suppressed" : "unsuppressed")}."
            : $"Failed to change suppression state of '{featureName}'.";
    });

    [McpServerTool, Description("Rename a feature in the active document.")]
    public string RenameFeature(
        [Description("Current feature name")] string currentName,
        [Description("New feature name")] string newName) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();
        dynamic? feat = doc.FeatureByName(currentName);
        if (feat is null)
            return $"Feature '{currentName}' not found.";

        feat.Name = newName;
        return $"Feature renamed: '{currentName}' → '{newName}'.";
    });

    [McpServerTool, Description("Delete a named feature from the active document.")]
    public string DeleteFeature(
        [Description("Exact name of the feature to delete")] string featureName) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();

        bool selected = doc.Extension.SelectByID2(featureName, "BODYFEATURE", 0, 0, 0, false, 0, null, 0);
        if (!selected)
            return $"Feature '{featureName}' not found or could not be selected.";

        bool ok = doc.Extension.DeleteSelection2(0);
        return ok ? $"Feature '{featureName}' deleted." : $"Failed to delete '{featureName}'.";
    });

    [McpServerTool, Description("Get mass properties (mass, volume, surface area, center of mass) of the active Part or Assembly.")]
    public string GetMassProperties() => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();

        dynamic? massProp = doc.Extension.CreateMassProperty();
        if (massProp is null)
            return "Could not compute mass properties (unsupported document type or no geometry).";

        double[] com = (double[])massProp.CenterOfMass;
        var result = new MassProperties(
            MassKg: massProp.Mass,
            VolumeM3: massProp.Volume,
            SurfaceAreaM2: massProp.SurfaceArea,
            CenterOfMassM: com
        );
        return ToolRunner.ToJson(result);
    });
}
