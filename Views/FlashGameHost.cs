using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace DDTankLauncher.Views;

/// <summary>
/// Flash 游戏宿主 - 独立 STA 线程
/// </summary>
public static class FlashGameHost
{
    private static readonly string LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "flash_debug.log");
    private static System.Windows.Forms.Form _currentForm;
    
    public static bool IsRunning => _currentForm != null && !_currentForm.IsDisposed;
    
    private static void Log(string msg)
    {
        try { File.AppendAllText(LogFile, $"[{DateTime.Now:HH:mm:ss}] {msg}\n"); } catch { }
    }
    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ACTCTX
    {
        public int cbSize;
        public uint dwFlags;
        public string lpSource;
        public ushort wProcessorArchitecture;
        public ushort wLangId;
        public string lpAssemblyDirectory;
        public string lpResourceName;
        public string lpApplicationName;
    }
    
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateActCtx(ref ACTCTX actctx);
    [DllImport("kernel32.dll")]
    private static extern bool ActivateActCtx(IntPtr hActCtx, out IntPtr lpCookie);
    [DllImport("kernel32.dll")]
    private static extern bool DeactivateActCtx(int dwFlags, IntPtr cookie);
    [DllImport("kernel32.dll")]
    private static extern void ReleaseActCtx(IntPtr hActCtx);
    
    private static IntPtr _actCtx = IntPtr.Zero;
    
    public static void ActivateFlashContext()
    {
        try
        {
            string manifestPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "flash", "Flash64.manifest");
            if (!File.Exists(manifestPath)) return;
            
            var actctx = new ACTCTX
            {
                cbSize = Marshal.SizeOf(typeof(ACTCTX)),
                lpSource = manifestPath
            };
            
            _actCtx = CreateActCtx(ref actctx);
            if (_actCtx != new IntPtr(-1))
            {
                ActivateActCtx(_actCtx, out _);
            }
        }
        catch { }
    }
    
    public static void DeactivateFlashContext()
    {
        try
        {
            if (_actCtx != IntPtr.Zero && _actCtx != new IntPtr(-1))
            {
                DeactivateActCtx(0, IntPtr.Zero);
                ReleaseActCtx(_actCtx);
                _actCtx = IntPtr.Zero;
            }
        }
        catch { }
    }
    
    [DllImport("atl.dll")] private static extern bool AtlAxWinInit();
    [DllImport("atl.dll")] private static extern int AtlAxGetControl(IntPtr hWnd, out IntPtr ppUnk);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(uint ex, string cls, string name, uint style, int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr inst, IntPtr param);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] private static extern bool UpdateWindow(IntPtr h);
    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(IntPtr r, int f);
    [DllImport("ole32.dll")]
    private static extern void CoUninitialize();
    
    public static void Run(string title, string url)
    {
        Log("=== Run started ===");
        try
        {
            CoInitializeEx(IntPtr.Zero, 0x2);
            AtlAxWinInit();
            
            _currentForm = new System.Windows.Forms.Form();
            _currentForm.Text = title;
            _currentForm.ClientSize = new System.Drawing.Size(1000, 600);
            _currentForm.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            _currentForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            _currentForm.MaximizeBox = false;
            _currentForm.FormClosing += (s, e) => _currentForm = null;
            
            IntPtr ax = CreateWindowEx(0, "AtlAxWin", "{D27CDB6E-AE6D-11CF-96B8-444553540000}",
                0x40000000 | 0x10000000, 0, 0, 1000, 600, _currentForm.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            
            if (ax != IntPtr.Zero)
            {
                ShowWindow(ax, 5);
                UpdateWindow(ax);
                Thread.Sleep(1500);
                
                IntPtr punk = IntPtr.Zero;
                if (AtlAxGetControl(ax, out punk) == 0 && punk != IntPtr.Zero)
                {
                    var flash = Marshal.GetObjectForIUnknown(punk);
                    Marshal.Release(punk);
                    if (flash != null)
                    {
                        dynamic d = flash;
                        try { d.wmode = "direct"; } catch { }
                        d.Movie = url;
                    }
                }
            }
            
            Log("Starting message loop...");
            _currentForm.Show();
            System.Windows.Forms.Application.Run(_currentForm);
        }
        catch (Exception ex) { Log($"ERROR: {ex}"); }
        finally { DeactivateFlashContext(); CoUninitialize(); }
    }
    
    public static void Stop()
    {
        try
        {
            if (_currentForm != null && !_currentForm.IsDisposed)
            {
                _currentForm.Close();
                _currentForm = null;
            }
        }
        catch { }
    }
}
