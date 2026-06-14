using System.Windows;

namespace DDTankLauncher.Views;

public partial class DuplicateAccountDialog : Window
{
    public ImportAction Result { get; private set; } = ImportAction.Cancel;

    public DuplicateAccountDialog()
    {
        InitializeComponent();
    }

    public void SetMessage(string message)
    {
        MessageText.Text = message;
    }

    private void OverwriteBtn_Click(object sender, RoutedEventArgs e)
    {
        Result = ImportAction.Overwrite;
        DialogResult = true;
        Close();
    }

    private void SkipBtn_Click(object sender, RoutedEventArgs e)
    {
        Result = ImportAction.SkipDuplicates;
        DialogResult = true;
        Close();
    }

    private void AddAllBtn_Click(object sender, RoutedEventArgs e)
    {
        Result = ImportAction.AddAll;
        DialogResult = true;
        Close();
    }
}