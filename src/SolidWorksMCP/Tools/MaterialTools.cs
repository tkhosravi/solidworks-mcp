using ModelContextProtocol.Server;
using System.ComponentModel;
using SolidWorksMCP.Core;
using SolidWorksMCP.Services;

namespace SolidWorksMCP.Tools;

[McpServerToolType]
public sealed class MaterialTools(SolidWorksConnectionService sw)
{
    private const string DefaultDatabase = "SOLIDWORKS Materials";

    [McpServerTool, Description("Get the material assigned to the active part.")]
    public string GetMaterial(
        [Description("Configuration name, or empty for the active configuration")] string configuration = "") => ToolRunner.Run(() =>
    {
        dynamic doc = RequirePart();
        string config = string.IsNullOrEmpty(configuration)
            ? (string)doc.ConfigurationManager.ActiveConfiguration.Name
            : configuration;

        string database = "";
        string? material = doc.GetMaterialPropertyName2(config, ref database);
        return string.IsNullOrEmpty(material)
            ? "No material assigned."
            : $"Material: {material} (database: {database}, configuration: {config})";
    });

    [McpServerTool, Description("Assign a material to the active part, e.g. '1060 Alloy', 'AISI 304', 'ABS', 'Plain Carbon Steel'.")]
    public string SetMaterial(
        [Description("Material name exactly as it appears in the SOLIDWORKS Materials database")] string materialName,
        [Description("Configuration name, or empty for the active configuration")] string configuration = "",
        [Description("Material database name. Default 'SOLIDWORKS Materials'.")] string database = DefaultDatabase) => ToolRunner.Run(() =>
    {
        dynamic doc = RequirePart();
        string config = string.IsNullOrEmpty(configuration)
            ? (string)doc.ConfigurationManager.ActiveConfiguration.Name
            : configuration;

        doc.SetMaterialPropertyName2(config, database, materialName);
        doc.EditRebuild3();
        return $"Material '{materialName}' assigned (configuration: {config}).";
    });

    private dynamic RequirePart()
    {
        dynamic doc = sw.GetActiveDoc();
        if ((int)doc.GetType() != SwConstants.DocPart)
            throw new InvalidOperationException("The active document is not a part — materials apply to parts.");
        return doc;
    }
}
