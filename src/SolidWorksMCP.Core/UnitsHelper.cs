namespace SolidWorksMCP.Core;

/// <summary>
/// The SolidWorks API always works in system units: meters for lengths,
/// radians for angles, kilograms for mass. Tools accept the units users
/// actually think in (mm, degrees) and convert at the boundary.
/// </summary>
public static class UnitsHelper
{
    public static double MmToMeters(double mm) => mm / 1000.0;
    public static double MetersToMm(double meters) => meters * 1000.0;
    public static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
    public static double RadiansToDegrees(double radians) => radians * 180.0 / Math.PI;

    /// <summary>
    /// Heuristic used for dimension reporting: angular dimension names in SW
    /// are radians, linear ones meters. Callers that know the dimension type
    /// should convert explicitly instead.
    /// </summary>
    public static double SystemValueToMm(double systemValue) => MetersToMm(systemValue);
}
