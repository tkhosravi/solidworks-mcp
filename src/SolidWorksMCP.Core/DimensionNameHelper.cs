namespace SolidWorksMCP.Core;

/// <summary>
/// Helpers around SolidWorks dimension naming ("D1@Sketch1@Part.SLDPRT").
/// IModelDoc2::Parameter wants at least "Name@Feature"; users often type just
/// "D1" or paste the full 3-part name from the UI.
/// </summary>
public static class DimensionNameHelper
{
    /// <summary>
    /// Normalizes a user-supplied dimension reference to the "Name@Feature"
    /// form Parameter() accepts, dropping the trailing "@Document" part if
    /// present. Throws ArgumentException when no feature part is given.
    /// </summary>
    public static string Normalize(string dimensionName)
    {
        var parts = (dimensionName ?? string.Empty).Trim().Split('@', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            >= 2 => $"{parts[0]}@{parts[1]}",
            _ => throw new ArgumentException(
                $"Dimension '{dimensionName}' must include the owning feature, e.g. 'D1@Sketch1'."),
        };
    }

    public static bool LooksAngular(string dimensionName) =>
        dimensionName.TrimStart().StartsWith("A", StringComparison.OrdinalIgnoreCase);
}
