using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DDTankLauncher.Views;

public partial class CaptchaWindow : Window
{
    public string CaptchaCode { get; private set; } = string.Empty;
    private readonly byte[] _captchaImageBytes;
    
    public CaptchaWindow(byte[] captchaImageBytes)
    {
        InitializeComponent();
        _captchaImageBytes = captchaImageBytes;
        LoadCaptchaImage();
    }
    
    private void LoadCaptchaImage()
    {
        try
        {
            if (_captchaImageBytes == null || _captchaImageBytes.Length == 0)
            {
                CaptchaImage.Source = null;
                return;
            }
            
            var bitmap = new BitmapImage();
            using (var ms = new MemoryStream(_captchaImageBytes))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();
            }
            CaptchaImage.Source = bitmap;
        }
        catch 
        {
            CaptchaImage.Source = null;
        }
    }
    
    public void UpdateCaptcha(byte[] newImageBytes)
    {
        _captchaImageBytes.CopyTo(newImageBytes, 0);
        LoadCaptchaImage();
    }
    
    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        CaptchaCode = CaptchaBox.Text.Trim();
        if (string.IsNullOrEmpty(CaptchaCode))
        {
            System.Windows.MessageBox.Show("请输入验证码", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }
    
    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshRequested?.Invoke(this, EventArgs.Empty);
    }
    
    public event EventHandler? RefreshRequested;
}
