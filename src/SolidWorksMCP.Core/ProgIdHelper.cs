namespace SolidWorksMCP.Core;

/// <summary>
/// SolidWorks registers a versioned COM ProgID of the form
/// <c>SldWorks.Application.NN</c> where NN = (release year - 1992).
/// SW 2025 → 33, SW 2026 → 34, etc.
/// </summary>
public static class ProgIdHelper
{
    public const string GenericProgId = "SldWorks.Application";
    private const int BaseYear = 1992;

    public static int VersionSuffix(int releaseYear)
    {
        if (releaseYear < 1995)
            throw new ArgumentOutOfRangeException(nameof(releaseYear),
                "SolidWorks release years start at 1995.");
        return releaseYear - BaseYear;
    }

    public static string VersionedProgId(int releaseYear) =>
        $"{GenericProgId}.{VersionSuffix(releaseYear)}";

    /// <summary>
    /// Candidate ProgIDs to try in order: the versioned one first (attaches to
    /// the exact release), then the generic alias as fallback.
    /// </summary>
    public static IReadOnlyList<string> Candidates(int releaseYear) =>
        new[] { VersionedProgId(releaseYear), GenericProgId };
}
