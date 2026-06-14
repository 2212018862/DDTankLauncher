using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DDTankLauncher.Views;

/// <summary>
/// 直接嵌入 Flash.ocx ActiveX 控件
/// </summary>
public class FlashHostControl : UserControl
{
    private object? _flashObject;
    
    public FlashHostControl()
    {
        // 创建 ShockwaveFlash 对象
        Type? flashType = Type.GetTypeFromCLSID(new Guid("D27CDB6E-AE6D-11CF-96B8-444553540000"));
        if (flashType != null)
        {
            _flashObject = Activator.CreateInstance(flashType);
        }
    }
    
    public void LoadUrl(string url)
    {
        if (_flashObject != null)
        {
            var type = _flashObject.GetType();
            type.InvokeMember("Movie", BindingFlags.SetProperty, null, _flashObject, new object[] { url });
            type.InvokeMember("quality", BindingFlags.SetProperty, null, _flashObject, new object[] { "high" });
            type.InvokeMember("wmode", BindingFlags.SetProperty, null, _flashObject, new object[] { "direct" });
        }
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing && _flashObject != null)
        {
            Marshal.ReleaseComObject(_flashObject);
            _flashObject = null;
        }
        base.Dispose(disposing);
    }
}
