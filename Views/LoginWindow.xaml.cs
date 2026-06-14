using System.Windows;

namespace DDTankLauncher.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }
    
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
