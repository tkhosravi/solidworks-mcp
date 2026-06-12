using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SolidWorksMCP.Core;
using SolidWorksMCP.Services;

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
