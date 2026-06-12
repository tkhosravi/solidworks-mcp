using ModelContextProtocol.Server;
using System.ComponentModel;
using SolidWorksMCP.Core;
using SolidWorksMCP.Core.Models;
using SolidWorksMCP.Services;

namespace SolidWorksMCP.Tools;

[McpServerToolType]
public sealed class AssemblyTools(SolidWorksConnectionService sw)
{
    [McpServerTool, Description("List the components of the active assembly (top level by default).")]
    public string ListComponents(
        [Description("true = only top-level components, false = all components recursively")] bool topLevelOnly = true) => ToolRunner.Run(() =>
    {
        dynamic doc = RequireAssembly();

        object? compsObj = doc.GetComponents(topLevelOnly);
        if (compsObj is not object[] comps || comps.Length == 0)
            return "The assembly contains no components.";

        var result = new List<ComponentInfo>();
        foreach (dynamic comp in comps)
        {
            result.Add(new ComponentInfo(
                Name: comp.Name2,
                Path: comp.GetPathName(),
                Configuration: comp.ReferencedConfiguration,
                SuppressionState: SwConstants.ComponentStateName(comp.GetSuppression()),
                IsFixed: comp.IsFixed()
            ));
        }
        return ToolRunner.ToJson(result);
    });

    [McpServerTool, Description("Suppress or resolve a component of the active assembly by name (use the Name2 returned by ListComponents, e.g. 'Bracket-1').")]
    public string SetComponentSuppression(
        [Description("Component name, e.g. 'Bracket-1'")] string componentName,
        [Description("true to suppress, false to resolve")] bool suppress) => ToolRunner.Run(() =>
    {
        dynamic doc = RequireAssembly();
        dynamic? comp = FindComponent(doc, componentName);
        if (comp is null)
            return $"Component '{componentName}' not found.";

        int state = suppress ? SwConstants.ComponentSuppressed : SwConstants.ComponentFullyResolved;
        int result = comp.SetSuppression2(state);
        // swSuppressionError_e: 1 = ok
        return result == 1
            ? $"Component '{componentName}' is now {(suppress ? "suppressed" : "resolved")}."
            : $"Failed to change state of '{componentName}' (error code {result}).";
    });

    [McpServerTool, Description("Insert an existing part or assembly file as a component into the active assembly, at given coordinates (millimeters).")]
    public string InsertComponent(
        [Description("Absolute path to the .sldprt or .sldasm file to insert")] string filePath,
        [Description("X position in mm")] double x = 0,
        [Description("Y position in mm")] double y = 0,
        [Description("Z position in mm")] double z = 0) => ToolRunner.Run(() =>
    {
        if (!File.Exists(filePath))
            return $"File not found: {filePath}";

        dynamic doc = RequireAssembly();

        // The component's document must be loaded in memory first
        var app = sw.GetApp();
        dynamic spec = app.GetOpenDocSpec(filePath);
        spec.Silent = true;
        app.OpenDoc7(spec);

        dynamic? comp = doc.AddComponent5(
            filePath, 0 /* config option: use active */, "", false, "",
            UnitsHelper.MmToMeters(x), UnitsHelper.MmToMeters(y), UnitsHelper.MmToMeters(z));

        return comp is null
            ? $"Failed to insert '{filePath}'."
            : $"Component '{comp.Name2}' inserted at ({x}, {y}, {z}) mm.";
    });

    [McpServerTool, Description("List the mates of the active assembly.")]
    public string ListMates() => ToolRunner.Run(() =>
    {
        dynamic doc = RequireAssembly();

        var mates = new List<MateInfo>();
        dynamic? feat = doc.FirstFeature();
        while (feat is not null)
        {
            if ((string)feat.GetTypeName2() == "MateGroup")
            {
                dynamic? sub = feat.GetFirstSubFeature();
                while (sub is not null)
                {
                    mates.Add(new MateInfo(sub.Name, sub.GetTypeName2()));
                    sub = sub.GetNextSubFeature();
                }
            }
            feat = feat.GetNextFeature();
        }

        return mates.Count == 0 ? "No mates found." : ToolRunner.ToJson(mates);
    });

    private dynamic RequireAssembly()
    {
        dynamic doc = sw.GetActiveDoc();
        if ((int)doc.GetType() != SwConstants.DocAssembly)
            throw new InvalidOperationException("The active document is not an assembly.");
        return doc;
    }

    private static dynamic? FindComponent(dynamic assemblyDoc, string name)
    {
        object? compsObj = assemblyDoc.GetComponents(false);
        if (compsObj is not object[] comps)
            return null;
        foreach (dynamic comp in comps)
        {
            if (string.Equals((string)comp.Name2, name, StringComparison.OrdinalIgnoreCase))
                return comp;
        }
        return null;
    }
}
