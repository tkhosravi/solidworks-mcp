namespace SolidWorksMCP.Core.Models;

public record DocumentInfo(
    string Title,
    string Path,
    string Type,          // Part, Assembly, Drawing
    bool IsModified
);

public record FeatureInfo(
    string Name,
    string TypeName,
    bool IsSuppressed
);

public record MassProperties(
    double MassKg,
    double VolumeM3,
    double SurfaceAreaM2,
    double[] CenterOfMassM   // [x, y, z]
);

public record ConfigurationInfo(
    string Name,
    bool IsActive,
    string? ParentName
);

public record CustomProperty(
    string Name,
    string Value,
    string ResolvedValue
);

public record ComponentInfo(
    string Name,
    string Path,
    string Configuration,
    string SuppressionState,
    bool IsFixed
);

public record MateInfo(
    string Name,
    string TypeName
);

public record DimensionInfo(
    string FullName,        // e.g. "D1@Sketch1@Part1.SLDPRT"
    double ValueMm,         // linear dimensions in millimeters
    double SystemValue      // raw SW system value (meters / radians)
);

public record EquationInfo(
    int Index,
    string Equation,
    bool IsGlobalVariable
);

public record SheetInfo(
    string Name,
    bool IsActive,
    string[] ViewNames
);
