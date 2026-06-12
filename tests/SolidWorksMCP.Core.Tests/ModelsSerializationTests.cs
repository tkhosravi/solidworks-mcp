using System.Text.Json;
using SolidWorksMCP.Core.Models;
using Xunit;

namespace SolidWorksMCP.Core.Tests;

/// <summary>
/// The records are what tools serialize back to the LLM — make sure the JSON
/// round-trips and keeps the property names tools document.
/// </summary>
public class ModelsSerializationTests
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    [Fact]
    public void DocumentInfo_RoundTrips()
    {
        var original = new DocumentInfo("bracket.SLDPRT", @"C:\m\bracket.SLDPRT", "Part", true);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DocumentInfo>(json);

        Assert.Equal(original, restored);
        Assert.Contains("\"Title\"", json);
        Assert.Contains("\"IsModified\"", json);
    }

    [Fact]
    public void MassProperties_SerializesCenterOfMassAsArray()
    {
        var props = new MassProperties(1.5, 0.0002, 0.05, [0.01, 0.02, 0.03]);

        var json = JsonSerializer.Serialize(props, Options);
        var restored = JsonSerializer.Deserialize<MassProperties>(json)!;

        Assert.Equal(3, restored.CenterOfMassM.Length);
        Assert.Equal(props.MassKg, restored.MassKg);
    }

    [Fact]
    public void ComponentInfo_RoundTrips()
    {
        var comp = new ComponentInfo("Bracket-1", @"C:\m\bracket.SLDPRT", "Default", "Resolved", false);

        var json = JsonSerializer.Serialize(comp);
        Assert.Equal(comp, JsonSerializer.Deserialize<ComponentInfo>(json));
    }

    [Fact]
    public void EquationInfo_RoundTrips()
    {
        var eq = new EquationInfo(0, "\"Width\" = 50mm", true);

        var json = JsonSerializer.Serialize(eq);
        Assert.Equal(eq, JsonSerializer.Deserialize<EquationInfo>(json));
    }
}
