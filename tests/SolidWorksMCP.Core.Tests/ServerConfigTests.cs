using SolidWorksMCP.Core;
using Xunit;

namespace SolidWorksMCP.Core.Tests;

public class ServerConfigTests
{
    private static Func<string, string?> Env(params (string Key, string Value)[] vars) =>
        key => vars.FirstOrDefault(v => v.Key == key).Value;

    [Fact]
    public void FromEnvironment_WithNoVariables_UsesDefaults()
    {
        var config = ServerConfig.FromEnvironment(_ => null);

        Assert.Equal(2025, config.ReleaseYear);
        Assert.Equal(@"C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\SLDWORKS.exe", config.ExePath);
        Assert.False(config.AutoStart);
        Assert.Equal(90, config.StartTimeoutSeconds);
    }

    [Fact]
    public void FromEnvironment_ReadsAllVariables()
    {
        var config = ServerConfig.FromEnvironment(Env(
            (ServerConfig.VersionVar, "2026"),
            (ServerConfig.ExePathVar, @"D:\SW\SLDWORKS.exe"),
            (ServerConfig.AutoStartVar, "true"),
            (ServerConfig.StartTimeoutVar, "120")));

        Assert.Equal(2026, config.ReleaseYear);
        Assert.Equal(@"D:\SW\SLDWORKS.exe", config.ExePath);
        Assert.True(config.AutoStart);
        Assert.Equal(120, config.StartTimeoutSeconds);
    }

    [Theory]
    [InlineData("abcd")]
    [InlineData("1980")]
    [InlineData("33")]    // ProgID suffix, not a year
    public void FromEnvironment_RejectsInvalidVersion(string version)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ServerConfig.FromEnvironment(Env((ServerConfig.VersionVar, version))));

        Assert.Contains(ServerConfig.VersionVar, ex.Message);
    }

    [Fact]
    public void FromEnvironment_VersionFeedsProgId()
    {
        var config = ServerConfig.FromEnvironment(Env((ServerConfig.VersionVar, "2024")));

        Assert.Equal("SldWorks.Application.32", ProgIdHelper.VersionedProgId(config.ReleaseYear));
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("1", true)]
    [InlineData("yes", true)]
    [InlineData("on", true)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    [InlineData("anything-else", false)]
    public void FromEnvironment_ParsesAutoStartLeniently(string raw, bool expected)
    {
        var config = ServerConfig.FromEnvironment(Env((ServerConfig.AutoStartVar, raw)));

        Assert.Equal(expected, config.AutoStart);
    }

    [Fact]
    public void FromEnvironment_StripsQuotesAroundExePath()
    {
        var config = ServerConfig.FromEnvironment(Env(
            (ServerConfig.ExePathVar, "\"C:\\Program Files\\SOLIDWORKS Corp\\SOLIDWORKS\\SLDWORKS.exe\"")));

        Assert.Equal(@"C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\SLDWORKS.exe", config.ExePath);
    }

    [Fact]
    public void FromEnvironment_RejectsExePathNotPointingToExe()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ServerConfig.FromEnvironment(Env(
                (ServerConfig.ExePathVar, @"C:\Program Files\SOLIDWORKS Corp"))));

        Assert.Contains("SLDWORKS.exe", ex.Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("4")]
    [InlineData("-10")]
    [InlineData("soon")]
    public void FromEnvironment_RejectsInvalidTimeout(string timeout)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ServerConfig.FromEnvironment(Env((ServerConfig.StartTimeoutVar, timeout))));

        Assert.Contains(ServerConfig.StartTimeoutVar, ex.Message);
    }
}
