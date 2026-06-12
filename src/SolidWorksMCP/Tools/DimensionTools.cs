using ModelContextProtocol.Server;
using System.ComponentModel;
using SolidWorksMCP.Core;
using SolidWorksMCP.Core.Models;
using SolidWorksMCP.Services;

namespace SolidWorksMCP.Tools;

[McpServerToolType]
public sealed class DimensionTools(SolidWorksConnectionService sw)
{
    [McpServerTool, Description("Read a dimension value from the active document. Use the full name shown in SolidWorks, e.g. 'D1@Sketch1' or 'D1@Boss-Extrude1'.")]
    public string GetDimension(
        [Description("Dimension name, e.g. 'D1@Sketch1'")] string dimensionName) => ToolRunner.Run(() =>
    {
        dynamic dim = FindDimension(dimensionName);
        double sysValue = dim.SystemValue; // meters or radians

        var info = new DimensionInfo(
            FullName: dim.FullName,
            ValueMm: UnitsHelper.MetersToMm(sysValue),
            SystemValue: sysValue
        );
        return ToolRunner.ToJson(info)
            + "\nNote: ValueMm assumes a linear dimension; for angles, SystemValue is in radians.";
    });

    [McpServerTool, Description("Change a dimension value in the active document and rebuild. Linear values in millimeters, angular values in degrees.")]
    public string SetDimension(
        [Description("Dimension name, e.g. 'D1@Sketch1'")] string dimensionName,
        [Description("New value (mm for lengths, degrees for angles)")] double value,
        [Description("Is this an angular dimension? Default false.")] bool isAngle = false) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();
        dynamic dim = FindDimension(dimensionName);

        double sysValue = isAngle ? UnitsHelper.DegreesToRadians(value) : UnitsHelper.MmToMeters(value);
        int result = dim.SetSystemValue3(sysValue, SwConstants.SetValueInThisConfiguration, null);
        // swSetValueReturnStatus_e: 0 = success
        if (result != 0)
            return $"Failed to set '{dimensionName}' (status {result}).";

        doc.EditRebuild3();
        return $"'{dimensionName}' set to {value} {(isAngle ? "°" : "mm")} and model rebuilt.";
    });

    [McpServerTool, Description("List the equations and global variables of the active document.")]
    public string ListEquations() => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();
        dynamic eqMgr = doc.GetEquationMgr();
        int count = eqMgr.GetCount();
        if (count == 0)
            return "No equations defined.";

        var equations = new List<EquationInfo>();
        for (int i = 0; i < count; i++)
        {
            equations.Add(new EquationInfo(
                Index: i,
                Equation: eqMgr.Equation[i],
                IsGlobalVariable: eqMgr.GlobalVariable[i]
            ));
        }
        return ToolRunner.ToJson(equations);
    });

    [McpServerTool, Description("Add an equation or global variable to the active document. Examples: '\"Width\" = 50mm' (global variable) or '\"D1@Sketch1\" = \"Width\" * 2' (equation).")]
    public string AddEquation(
        [Description("The equation text, with quoted names, e.g. '\"D1@Sketch1\" = \"Width\" / 2'")] string equation) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();
        dynamic eqMgr = doc.GetEquationMgr();
        int index = eqMgr.Add2(-1, equation, true); // -1 = append, true = rebuild
        return index >= 0
            ? $"Equation added at index {index}: {equation}"
            : "Failed to add equation. Check the syntax (names must be double-quoted).";
    });

    [McpServerTool, Description("Modify an existing equation by index (see ListEquations for indices).")]
    public string SetEquation(
        [Description("Equation index from ListEquations")] int index,
        [Description("New equation text")] string equation) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();
        dynamic eqMgr = doc.GetEquationMgr();
        if (index < 0 || index >= (int)eqMgr.GetCount())
            return $"Index {index} out of range (0..{(int)eqMgr.GetCount() - 1}).";

        eqMgr.Equation[index] = equation;
        doc.EditRebuild3();
        return $"Equation {index} updated: {equation}";
    });

    [McpServerTool, Description("Delete an equation by index (see ListEquations for indices).")]
    public string DeleteEquation(
        [Description("Equation index from ListEquations")] int index) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();
        dynamic eqMgr = doc.GetEquationMgr();
        int result = eqMgr.Delete(index);
        return result >= 0 ? $"Equation {index} deleted." : $"Could not delete equation {index}.";
    });

    private dynamic FindDimension(string dimensionName)
    {
        dynamic doc = sw.GetActiveDoc();
        string normalized = DimensionNameHelper.Normalize(dimensionName);
        dynamic? param = doc.Parameter(normalized);
        return param ?? throw new ArgumentException(
            $"Dimension '{dimensionName}' not found. Use the 'Name@Feature' form, e.g. 'D1@Sketch1'.");
    }
}
