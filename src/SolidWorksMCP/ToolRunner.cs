using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace SolidWorksMCP;

/// <summary>
/// Wraps tool bodies so COM and validation failures come back to the LLM as
/// readable text instead of crashing the MCP call.
/// </summary>
internal static class ToolRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// SolidWorks' type library registers its member names in English (LCID
    /// 1033). On a non-English Windows, late-bound (`dynamic`) calls resolve
    /// names with the thread's LCID and fail with TYPE_E_ELEMENTNOTFOUND
    /// (0x8002802B) even though the member exists. Forcing the calling thread
    /// to en-US makes IDispatch name resolution deterministic everywhere.
    /// </summary>
    private static readonly CultureInfo ComCulture = CultureInfo.GetCultureInfo("en-US");

    public static string Run(Func<string> body)
    {
        var thread = Thread.CurrentThread;
        var previousCulture = thread.CurrentCulture;
        thread.CurrentCulture = ComCulture;
        try
        {
            return body();
        }
        catch (COMException ex)
        {
            return $"SolidWorks COM error 0x{ex.HResult:X8}: {ex.Message}";
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return ex.Message;
        }
        catch (Exception ex)
        {
            return $"Unexpected error: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            thread.CurrentCulture = previousCulture;
        }
    }

    public static string ToJson<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);
}
