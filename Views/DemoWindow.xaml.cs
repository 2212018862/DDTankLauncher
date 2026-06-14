using System.Runtime.InteropServices;
using System.Windows;

namespace DDTankLauncher.Views;

public partial class DemoWindow : Window
{
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
    
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassW(ref WNDCLASS lpWndClass);
    
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowExW(
        uint dwExStyle, string lpClassName, string lpWindowName,
        uint dwStyle, int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);
    
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    
    [DllImport("user32.dll")]
    private static extern bool UpdateWindow(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
    
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandleW(string lpModuleName);
    
    private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
    private const uint WS_VISIBLE = 0x10000000;
    private const uint CS_HREDRAW = 0x0002;
    private const uint CS_VREDRAW = 0x0001;
    private const int SW_SHOW = 5;
    
    // 保持委托引用防止GC回收
    private static readonly WndProcDelegate _wndProc = DefWindowProcW;
    
    private readonly List<IntPtr> _createdWindows = new();
    private readonly List<string> _windowDescriptions = new();
    
    public DemoWindow()
    {
        InitializeComponent();
        Closed += (s, e) => CleanupWindows();
    }
    
    private ushort RegisterCustomClass(string className)
    {
        IntPtr hInstance = GetModuleHandleW(null);
        
        var wndClass = new WNDCLASS
        {
            style = CS_HREDRAW | CS_VREDRAW,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            hInstance = hInstance,
            hbrBackground = (IntPtr)(1 + 1),
            lpszClassName = className
        };
        
        return RegisterClassW(ref wndClass);
    }
    
    private IntPtr CreateTestWindow(string className, string title, int x, int y)
    {
        RegisterCustomClass(className);
        
        IntPtr hWnd = CreateWindowExW(
            0, className, title,
            WS_OVERLAPPEDWINDOW | WS_VISIBLE,
            x, y, 800, 600,
            IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        
        if (hWnd != IntPtr.Zero)
        {
            ShowWindow(hWnd, SW_SHOW);
            UpdateWindow(hWnd);
            _createdWindows.Add(hWnd);
        }
        
        return hWnd;
    }
    
    private void Create36JBWindow(object sender, RoutedEventArgs e)
    {
        IntPtr hWnd = CreateTestWindow("36JBCOM_Browser", "[4399-132-测试]|1|测试|0|0|0", 100, 100);
        if (hWnd != IntPtr.Zero) { _windowDescriptions.Add("方案1: 36JBCOM_Browser ✓"); UpdateStatus(); }
    }
    
    private void CreateTangoWindow(object sender, RoutedEventArgs e)
    {
        IntPtr hWnd = CreateTestWindow("Tango3", "4399弹弹堂-测试-(132服)--4399网页游戏----专注精品页游的一线游戏平台 - 糖果游戏浏览器", 200, 100);
        if (hWnd != IntPtr.Zero) { _windowDescriptions.Add("方案2: Tango3 ✓"); UpdateStatus(); }
    }
    
    private void CreateFlashWithTitle(object sender, RoutedEventArgs e)
    {
        IntPtr hWnd = CreateTestWindow("ShockwaveFlash", "4399弹弹堂-测试-(132服)--4399网页游戏----专注精品页游的一线游戏平台", 300, 100);
        if (hWnd != IntPtr.Zero) { _windowDescriptions.Add("方案3: ShockwaveFlash + 游戏标题 ✓"); UpdateStatus(); }
    }
    
    private void Create36JBWithFlashChild(object sender, RoutedEventArgs e)
    {
        IntPtr parentHWnd = CreateTestWindow("36JBCOM_Browser", "[4399-132-测试]|1|测试|0|0|0", 100, 200);
        if (parentHWnd != IntPtr.Zero)
        {
            RegisterCustomClass("ShockwaveFlash");
            CreateWindowExW(0, "ShockwaveFlash", "", WS_VISIBLE | 0x40000000, 0, 0, 780, 560, parentHWnd, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            _windowDescriptions.Add("方案4: 36JBCOM_Browser + ShockwaveFlash子窗口 ✓");
            UpdateStatus();
        }
    }
    
    private void CreateMacromediaWindow(object sender, RoutedEventArgs e)
    {
        IntPtr hWnd = CreateTestWindow("MacromediaFlashPlayerActiveX", "Adobe Flash Player 34", 400, 100);
        if (hWnd != IntPtr.Zero) { _windowDescriptions.Add("方案5: MacromediaFlashPlayerActiveX ✓"); UpdateStatus(); }
    }
    
    private void CreateFullStructure(object sender, RoutedEventArgs e)
    {
        IntPtr mainHWnd = CreateTestWindow("36JBCOM_Browser", "[4399-132-测试号]|3|测试号|7084110|533070|2239556", 150, 150);
        if (mainHWnd != IntPtr.Zero)
        {
            RegisterCustomClass("_EL_PicBox");
            RegisterCustomClass("ShockwaveFlash");
            IntPtr picBox = CreateWindowExW(0, "_EL_PicBox", "", WS_VISIBLE | 0x40000000, 0, 0, 780, 560, mainHWnd, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            CreateWindowExW(0, "ShockwaveFlash", "", WS_VISIBLE | 0x40000000, 0, 0, 780, 560, picBox, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            _windowDescriptions.Add("方案6: 完整36脚本大厅结构 ✓");
            UpdateStatus();
        }
    }
    
    private void UpdateStatus() => StatusText.Text = string.Join("\n", _windowDescriptions);
    
    private void CleanupWindows()
    {
        foreach (var hWnd in _createdWindows) try { DestroyWindow(hWnd); } catch { }
    }
}
public struct WNDCLASS { public uint style; public IntPtr lpfnWndProc; public int cbClsExtra; public int cbWndExtra; public IntPtr hInstance; public IntPtr hIcon; public IntPtr hCursor; public IntPtr hbrBackground; public string lpszMenuName; public string lpszClassName; }
