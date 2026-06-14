using System;
using System.Runtime.InteropServices;

// 测试 Flash COM 对象
Type? flashType = Type.GetTypeFromCLSID(new Guid("D27CDB6E-AE6D-11CF-96B8-444553540000"));
Console.WriteLine($"Flash type: {flashType}");

if (flashType != null)
{
    var flash = Activator.CreateInstance(flashType);
    Console.WriteLine($"Flash object: {flash}");
    
    if (flash != null)
    {
        // 设置属性
        flash.GetType().InvokeMember("Movie", System.Reflection.BindingFlags.SetProperty, null, flash, new object[] { "http://s132.ddt.1322game.com/Loading.swf?user=test&key=test" });
        flash.GetType().InvokeMember("quality", System.Reflection.BindingFlags.SetProperty, null, flash, new object[] { "high" });
        
        Console.WriteLine("Movie set");
        
        // 检查窗口
        System.Threading.Thread.Sleep(2000);
        
        // 枚举窗口
        EnumWindows((hWnd, lParam) =>
        {
            if (IsWindowVisible(hWnd))
            {
                var cls = new char[256];
                GetClassName(hWnd, cls, 256);
                string className = new string(cls).TrimEnd('\0');
                
                if (className.Contains("Shockwave") || className.Contains("Flash"))
                {
                    Console.WriteLine($"Found: {hWnd} - {className}");
                }
            }
            return true;
        }, IntPtr.Zero);
        
        Marshal.ReleaseComObject(flash);
    }
}

[DllImport("user32.dll")]
static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

[DllImport("user32.dll")]
static extern bool IsWindowVisible(IntPtr hWnd);

[DllImport("user32.dll")]
static extern int GetClassName(IntPtr hWnd, char[] lpClassName, int nMaxCount);

delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
