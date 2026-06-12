using ModelContextProtocol.Server;
using System.ComponentModel;
using SolidWorksMCP.Core.Models;
using SolidWorksMCP.Services;

namespace SolidWorksMCP.Tools;

[McpServerToolType]
public sealed class ConfigurationTools(SolidWorksConnectionService sw)
{
    [McpServerTool, Description("List all configurations of the active document and indicate which one is active.")]
    public string ListConfigurations() => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();
        string activeName = doc.ConfigurationManager.ActiveConfiguration.Name;

        object[] names = (object[])doc.GetConfigurationNames();
        var configs = new List<ConfigurationInfo>();
        foreach (string name in names.Cast<string>())
        {
            dynamic config = doc.GetConfigurationByName(name);
            dynamic? parent = config.GetParent();
            configs.Add(new ConfigurationInfo(
                Name: name,
                IsActive: string.Equals(name, activeName, StringComparison.Ordinal),
                ParentName: parent?.Name
            ));
        }
        return ToolRunner.ToJson(configs);
    });

    [McpServerTool, Description("Activate a configuration by name in the active document.")]
    public string ActivateConfiguration(
        [Description("Configuration name")] string name) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();
        bool ok = doc.ShowConfiguration2(name);
        return ok ? $"Configuration '{name}' activated." : $"Configuration '{name}' not found.";
    });

    [McpServerTool, Description("Create a new configuration in the active document.")]
    public string CreateConfiguration(
        [Description("Name for the new configuration")] string name,
        [Description("Optional comment")] string comment = "") => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();
        dynamic? config = doc.ConfigurationManager.AddConfiguration2(
            name, comment, "", 0, "", "", true);
        return config is null
            ? $"Failed to create configuration '{name}' (name may already exist)."
            : $"Configuration '{name}' created and activated.";
    });

    [McpServerTool, Description("Delete a configuration by name from the active document.")]
    public string DeleteConfiguration(
        [Description("Configuration name to delete")] string name) => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();
        bool ok = doc.DeleteConfiguration2(name);
        return ok ? $"Configuration '{name}' deleted."
                  : $"Could not delete '{name}' (active or last remaining configurations cannot be deleted).";
    });
}
