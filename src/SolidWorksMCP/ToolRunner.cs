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

    public static string Run(Func<string> body)
    {
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
    }

    public static string ToJson<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);
}
