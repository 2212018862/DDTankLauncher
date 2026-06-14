using System.Windows;
using System.Text;

namespace DDTankLauncher;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // 注册代码页编码提供程序
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
}
