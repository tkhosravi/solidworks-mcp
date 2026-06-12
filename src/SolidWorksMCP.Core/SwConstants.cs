namespace SolidWorksMCP.Core;

/// <summary>
/// Subset of the swconst.dll enumerations used by the server, so the main
/// project does not need the interop assembly at compile time (tools talk to
/// COM through <c>dynamic</c>). Values come from the SolidWorks 2025 API docs.
/// </summary>
public static class SwConstants
{
    // swDocumentTypes_e
    public const int DocPart = 1;
    public const int DocAssembly = 2;
    public const int DocDrawing = 3;

    // swSaveAsOptions_e
    public const int SaveAsOptionsSilent = 1;

    // swFeatureSuppressionAction_e
    public const int SuppressFeature = 0;
    public const int UnsuppressFeature = 1;

    // swInConfigurationOpts_e
    public const int AllConfigurations = 1;
    public const int ThisConfiguration = 2;

    // swComponentSuppressionState_e
    public const int ComponentSuppressed = 0;
    public const int ComponentLightweight = 1;
    public const int ComponentFullyResolved = 2;

    // swCustomInfoType_e
    public const int CustomInfoText = 30;

    // swCustomPropertyAddOption_e
    public const int CustomPropertyDeleteAndAdd = 1;

    // swUserPreferenceStringValue_e — default document templates
    public const int DefaultTemplatePart = 8;
    public const int DefaultTemplateAssembly = 9;
    public const int DefaultTemplateDrawing = 10;

    // swStandardViews_e (ShowNamedView2 view ids)
    public const int ViewFront = 1;
    public const int ViewBack = 2;
    public const int ViewLeft = 3;
    public const int ViewRight = 4;
    public const int ViewTop = 5;
    public const int ViewBottom = 6;
    public const int ViewIsometric = 7;

    // swEndConditions_e
    public const int EndCondBlind = 0;
    public const int EndCondThroughAll = 1;
    public const int EndCondMidPlane = 6;

    // swRebuildOptions / misc
    public const int SetValueInThisConfiguration = 2; // swSetValueInConfiguration_e

    /// <summary>Maps a swDocumentTypes_e value to a human-readable name.</summary>
    public static string DocTypeName(int swDocType) => swDocType switch
    {
        DocPart => "Part",
        DocAssembly => "Assembly",
        DocDrawing => "Drawing",
        _ => $"Unknown({swDocType})",
    };

    /// <summary>Maps a swComponentSuppressionState_e value to a readable name.</summary>
    public static string ComponentStateName(int state) => state switch
    {
        ComponentSuppressed => "Suppressed",
        ComponentLightweight => "Lightweight",
        ComponentFullyResolved => "Resolved",
        3 => "Resolved",          // swComponentResolved (legacy alias)
        4 => "FullyLightweight",
        _ => $"Unknown({state})",
    };

    /// <summary>Maps a standard view name (en/fr) to its swStandardViews_e id, or null.</summary>
    public static int? StandardViewId(string name) => name.Trim().ToLowerInvariant() switch
    {
        "front" or "face" => ViewFront,
        "back" or "arriere" or "arrière" => ViewBack,
        "left" or "gauche" => ViewLeft,
        "right" or "droite" => ViewRight,
        "top" or "dessus" => ViewTop,
        "bottom" or "dessous" => ViewBottom,
        "isometric" or "iso" or "isometrique" or "isométrique" => ViewIsometric,
        _ => null,
    };
}
