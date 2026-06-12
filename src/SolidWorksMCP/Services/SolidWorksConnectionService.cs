using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SolidWorksMCP.Core;

namespace SolidWorksMCP.Services;

/// <summary>
/// Manages the COM connection to a running SolidWorks instance.
/// By default SolidWorks must already be open — we attach via the Running
/// Object Table. With SOLIDWORKS_AUTO_START=true the server launches
/// SLDWORKS.exe (path from SOLIDWORKS_EXE_PATH) and waits for it instead.
/// </summary>
public sealed class SolidWorksConnectionService : IDisposable
{
    private readonly ILogger<SolidWorksConnectionService> _logger;
    private readonly ServerConfig _config;
    private dynamic? _swApp;
    private bool _disposed;

    public SolidWorksConnectionService(ILogger<SolidWorksConnectionService> logger, ServerConfig config)
    {
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Returns the live ISldWorks COM object, connecting on first call.
    /// Throws InvalidOperationException if SolidWorks is not reachable.
    /// </summary>
    public dynamic GetApp()
    {
        if (_swApp is not null)
        {
            try
            {
                // Quick liveness check — reading Revision throws if SW died
                _ = _swApp.RevisionNumber();
                return _swApp;
            }
            catch
            {
                _logger.LogWarning("Lost connection to SolidWorks, reconnecting…");
                ReleaseApp();
            }
        }

        _swApp = Connect();
        return _swApp;
    }

    /// <summary>
    /// Returns the active IModelDoc2, or throws with a user-friendly message
    /// when no document is open.
    /// </summary>
    public dynamic GetActiveDoc()
    {
        dynamic? doc = GetApp().ActiveDoc;
        return doc ?? throw new InvalidOperationException(
            "No active document. Open a part, assembly or drawing in SolidWorks first.");
    }

    /// <summary>Returns true if a SolidWorks instance is currently reachable.</summary>
    public bool IsConnected()
    {
        try { _ = GetApp(); return true; }
        catch { return false; }
    }

    private object Connect()
    {
        if (TryAttach(out var app))
            return app!;

        if (_config.AutoStart)
            return StartAndAttach();

        throw new InvalidOperationException(
            "SolidWorks is not running. Launch it first, or set the environment variable " +
            $"{ServerConfig.AutoStartVar}=true (and optionally {ServerConfig.ExePathVar}) " +
            "to let the server start it automatically.");
    }

    private object StartAndAttach()
    {
        if (!File.Exists(_config.ExePath))
            throw new InvalidOperationException(
                $"SolidWorks executable not found at '{_config.ExePath}'. " +
                $"Set {ServerConfig.ExePathVar} to the full path of SLDWORKS.exe.");

        _logger.LogInformation("Starting SolidWorks: {Exe}", _config.ExePath);
        Process.Start(new ProcessStartInfo(_config.ExePath) { UseShellExecute = true });

        var deadline = DateTime.UtcNow.AddSeconds(_config.StartTimeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            Thread.Sleep(2000);
            if (TryAttach(out var app))
            {
                _logger.LogInformation("SolidWorks started and connected.");
                return app!;
            }
        }

        throw new InvalidOperationException(
            $"SolidWorks was launched but did not become available within " +
            $"{_config.StartTimeoutSeconds}s. Increase {ServerConfig.StartTimeoutVar} if " +
            "your machine needs more time, then retry.");
    }

    private bool TryAttach(out object? app)
    {
        foreach (var progId in ProgIdHelper.Candidates(_config.ReleaseYear))
        {
            if (TryGetActiveComObject(progId, out app))
            {
                _logger.LogInformation("Connected to SolidWorks via {ProgId}", progId);
                return true;
            }
        }
        app = null;
        return false;
    }

    /// <summary>
    /// Marshal.GetActiveObject was removed from .NET (Core), so we query the
    /// Running Object Table through the underlying OLE APIs directly.
    /// </summary>
    private static bool TryGetActiveComObject(string progId, out object? instance)
    {
        instance = null;
        if (NativeMethods.CLSIDFromProgID(progId, out var clsid) != 0)
            return false;
        return NativeMethods.GetActiveObject(ref clsid, IntPtr.Zero, out instance) == 0
               && instance is not null;
    }

    private static class NativeMethods
    {
        [DllImport("ole32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int CLSIDFromProgID(string lpszProgID, out Guid pclsid);

        [DllImport("oleaut32.dll", ExactSpelling = true)]
        public static extern int GetActiveObject(
            ref Guid rclsid,
            IntPtr pvReserved,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);
    }

    private void ReleaseApp()
    {
        if (_swApp is not null)
        {
            try { Marshal.ReleaseComObject(_swApp); } catch { /* ignore */ }
            _swApp = null;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            ReleaseApp();
            _disposed = true;
        }
    }
}
