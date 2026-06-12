using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SolidWorksMCP.Core;
using SolidWorksMCP.Services;

// SolidWorks' COM type library exposes its member names in English. On a
// non-English Windows, late-bound (`dynamic`) calls would otherwise fail name
// resolution with TYPE_E_ELEMENTNOTFOUND. Default every thread to en-US so
// IDispatch lookups behave the same regardless of the machine's locale.
// (ToolRunner reasserts this per call as defence in depth.)
var comCulture = CultureInfo.GetCultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = comCulture;
Thread.CurrentThread.CurrentCulture = comCulture;

var builder = Host.CreateApplicationBuilder(args);

// stdout is reserved for the MCP protocol — all logs must go to stderr
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Configuration via environment variables (SOLIDWORKS_VERSION,
// SOLIDWORKS_EXE_PATH, SOLIDWORKS_AUTO_START, SOLIDWORKS_START_TIMEOUT).
// An invalid value throws here, so misconfiguration is visible in the MCP
// client logs instead of failing silently on the first tool call.
var config = ServerConfig.FromEnvironment(Environment.GetEnvironmentVariable);

builder.Services.AddSingleton(config);
builder.Services.AddSingleton<SolidWorksConnectionService>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var host = builder.Build();

host.Services.GetRequiredService<ILogger<Program>>().LogInformation(
    "SolidWorks MCP server starting — target release {Year}, auto-start: {AutoStart}",
    config.ReleaseYear, config.AutoStart);

await host.RunAsync();
