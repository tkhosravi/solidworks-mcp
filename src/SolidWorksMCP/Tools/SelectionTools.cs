using ModelContextProtocol.Server;
using System.ComponentModel;
using SolidWorksMCP.Services;

namespace SolidWorksMCP.Tools;

[McpServerToolType]
public sealed class SelectionTools(SolidWorksConnectionService sw)
{
    [McpServerTool, Description("Clear the current selection in the active document.")]
    public string ClearSelection() => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();
        doc.ClearSelection2(true);
        return "Selection cleared.";
    });

    [McpServerTool, Description("Select an entity by name in the active document. Common types: PLANE, FACE, EDGE, VERTEX, SKETCH, BODYFEATURE, COMPONENT, DIMENSION, AXIS.")]
    public string SelectByName(
        [Description("Name of the entity to select")] string name,
        [Description("Selection type string, e.g. 'FACE', 'EDGE', 'COMPONENT'. Default 'BODYFEATURE'.")] string selType = "BODYFEATURE",
        [Description("Append to the current selection instead of replacing it? Default false.")] bool append = false) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();

        bool ok = doc.Extension.SelectByID2(name, selType.ToUpperInvariant(), 0, 0, 0, append, 0, null, 0);
        return ok
            ? $"'{name}' selected ({selType})."
            : $"Could not select '{name}' (type: {selType}). Check the name and type.";
    });

    [McpServerTool, Description("Get the number and names of currently selected entities.")]
    public string GetSelection() => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();
        dynamic selMgr = doc.SelectionManager;
        int count = selMgr.GetSelectedObjectCount2(-1);
        if (count == 0)
            return "Nothing is selected.";

        var lines = new List<string> { $"{count} entit{(count > 1 ? "ies" : "y")} selected:" };
        for (int i = 1; i <= count; i++)
        {
            int type = selMgr.GetSelectedObjectType3(i, -1);
            lines.Add($"  {i}. selection type id = {type}");
        }
        return string.Join("\n", lines);
    });
}
