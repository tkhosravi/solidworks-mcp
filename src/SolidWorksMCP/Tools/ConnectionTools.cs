using ModelContextProtocol.Server;
using System.ComponentModel;
using SolidWorksMCP.Services;

namespace SolidWorksMCP.Tools;

[McpServerToolType]
public sealed class ConnectionTools(SolidWorksConnectionService sw)
{
    [McpServerTool, Description("Check whether a SolidWorks instance is currently reachable via COM, and report its version.")]
    public string CheckConnection() => ToolRunner.Run(() =>
    {
        if (!sw.IsConnected())
            return "Not connected — SolidWorks does not appear to be running.";

        var app = sw.GetApp();
        string revision = app.RevisionNumber();
        return $"Connected — SolidWorks revision {revision}";
    });

    [McpServerTool, Description("Run an existing SolidWorks VBA macro (.swp/.swb file). Escape hatch for operations not covered by other tools.")]
    public string RunMacro(
        [Description("Absolute path to the macro file (.swp)")] string macroPath,
        [Description("Module name inside the macro, e.g. 'Module1' or the macro file name")] string moduleName,
        [Description("Procedure to run, usually 'main'")] string procedureName = "main") => ToolRunner.Run(() =>
    {
        if (!File.Exists(macroPath))
            return $"Macro file not found: {macroPath}";

        var app = sw.GetApp();
        int err = 0;
        bool ok = app.RunMacro2(macroPath, moduleName, procedureName, 1 /* swRunMacroUnloadAfterRun */, ref err);
        return ok ? $"Macro '{Path.GetFileName(macroPath)}' executed."
                  : $"Macro failed (error code {err}).";
    });
}
