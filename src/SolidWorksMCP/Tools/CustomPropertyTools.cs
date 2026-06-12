using ModelContextProtocol.Server;
using System.ComponentModel;
using SolidWorksMCP.Core;
using SolidWorksMCP.Core.Models;
using SolidWorksMCP.Services;

namespace SolidWorksMCP.Tools;

[McpServerToolType]
public sealed class CustomPropertyTools(SolidWorksConnectionService sw)
{
    [McpServerTool, Description("List custom properties of the active document. Empty configuration name = document-level (file) properties.")]
    public string ListCustomProperties(
        [Description("Configuration name, or empty for document-level properties")] string configuration = "") => ToolRunner.Run(() =>
    {
        dynamic cpm = GetPropertyManager(configuration);

        object? namesObj = cpm.GetNames();
        if (namesObj is not object[] names || names.Length == 0)
            return "No custom properties found.";

        var props = new List<CustomProperty>();
        foreach (string name in names.Cast<string>())
        {
            string value = "", resolved = "";
            cpm.Get4(name, false, ref value, ref resolved);
            props.Add(new CustomProperty(name, value, resolved));
        }
        return ToolRunner.ToJson(props);
    });

    [McpServerTool, Description("Set (create or overwrite) a custom property on the active document.")]
    public string SetCustomProperty(
        [Description("Property name, e.g. 'Description', 'PartNumber', 'Material'")] string name,
        [Description("Property value. Can contain SW expressions like \"SW-Mass@Part1.SLDPRT\"")] string value,
        [Description("Configuration name, or empty for document-level properties")] string configuration = "") => ToolRunner.Run(() =>
    {
        dynamic cpm = GetPropertyManager(configuration);
        int result = cpm.Add3(name, SwConstants.CustomInfoText, value, SwConstants.CustomPropertyDeleteAndAdd);
        // swCustomInfoAddResult_e: 0 = AddedOrChanged
        return result == 0
            ? $"Property '{name}' = '{value}' set{ConfigSuffix(configuration)}."
            : $"Failed to set property '{name}' (result code {result}).";
    });

    [McpServerTool, Description("Delete a custom property from the active document.")]
    public string DeleteCustomProperty(
        [Description("Property name to delete")] string name,
        [Description("Configuration name, or empty for document-level properties")] string configuration = "") => ToolRunner.Run(() =>
    {
        dynamic cpm = GetPropertyManager(configuration);
        int result = cpm.Delete2(name);
        return result == 0
            ? $"Property '{name}' deleted{ConfigSuffix(configuration)}."
            : $"Property '{name}' not found{ConfigSuffix(configuration)}.";
    });

    private dynamic GetPropertyManager(string configuration)
    {
        dynamic doc = sw.GetActiveDoc();
        return doc.Extension.CustomPropertyManager[configuration ?? ""];
    }

    private static string ConfigSuffix(string configuration) =>
        string.IsNullOrEmpty(configuration) ? " (document level)" : $" (configuration '{configuration}')";
}
