using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DDTankLauncher.Models;
using DDTankLauncher.Services;

namespace DDTankLauncher.Views;

public enum ImportAction
{
    AddAll,
    Overwrite,
    SkipDuplicates,
    Cancel
}

public partial class MainWindow : Window
{
    private readonly ObservableCollection<GameAccount> _accounts = new();
    private readonly AccountManager36 _accountManager;
    private int _currentGroup = -1;
    private System.Windows.Controls.Button? _selectedButton = null;
    
    public MainWindow()
    {
        InitializeComponent();
        
        string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        _accountManager = new AccountManager36(dataDir);
        
        AccountList.ItemsSource = _accounts;
        LoadSavedAccounts();
        
        Loaded += MainWindow_Loaded;
    }
    
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        SwitchGroup(1);
        if (Group1Btn != null)
        {
            Group1Btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50));
            _selectedButton = Group1Btn;
        }
    }
    
    private void AddAccount_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddAccountWindow();
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            if (dialog.IsImportMode)
            {
                // 一键导入
                string sourceDir = @"C:\1210107875\36JB_COM_CONFIG\data";
                
                // 先读取要导入的账号
                var importedAccounts = _accountManager.ReadFrom36jb(sourceDir);
                
                // 检测重复账号
                var existingUsernames = _accountManager.Accounts.Select(a => a.Account).ToHashSet();
                var duplicateAccounts = importedAccounts.Where(a => existingUsernames.Contains(a.Account)).ToList();
                
                ImportAction action = ImportAction.AddAll;
                if (duplicateAccounts.Any())
                {
                    string message = $"检测到 {duplicateAccounts.Count} 个账号已存在于登录器中：\n";
                    message += string.Join("\n", duplicateAccounts.Take(5).Select(a => $"  - {a.Account}"));
                    if (duplicateAccounts.Count > 5)
                        message += $"\n  ...(还有 {duplicateAccounts.Count - 5} 个)";
                    message += "\n\n请选择处理方式：";
                    
                    var dupDialog = new DuplicateAccountDialog();
                    dupDialog.Owner = this;
                    dupDialog.SetMessage(message);
                    if (dupDialog.ShowDialog() == true)
                    {
                        action = dupDialog.Result;
                    }
                    else
                    {
                        action = ImportAction.Cancel;
                    }
                }
                
                if (action == ImportAction.Cancel)
                {
                    BottomStatus.Text = "❌ 导入已取消";
                    return;
                }
                
                // 根据用户选择处理导入
                foreach (var acc in importedAccounts)
                {
                    bool isDuplicate = existingUsernames.Contains(acc.Account);
                    
                    if (isDuplicate)
                    {
                        if (action == ImportAction.SkipDuplicates)
                            continue; // 跳过重复
                        else if (action == ImportAction.Overwrite)
                        {
                            // 覆盖现有账号
                            int existingIndex = _accountManager.Accounts.FindIndex(a => a.Account == acc.Account);
                            if (existingIndex >= 0)
                            {
                                _accountManager.Accounts[existingIndex] = acc;
                            }
                        }
                        else if (action == ImportAction.AddAll)
                        {
                            // 仍要添加，允许重复
                            _accountManager.Accounts.Add(acc);
                        }
                    }
                    else
                    {
                        // 添加新账号
                        _accountManager.Accounts.Add(acc);
                    }
                }
                
                // 重新加载分组
                _accountManager.Groups.Clear();
                var groupFiles = Directory.GetFiles(sourceDir, "*小组.txt");
                foreach (var file in groupFiles)
                {
                    string groupName = Path.GetFileNameWithoutExtension(file);
                    string content = File.ReadAllText(file, Encoding.GetEncoding("gb2312")).Trim();
                    if (!string.IsNullOrEmpty(content))
                    {
                        if (!_accountManager.Groups.ContainsKey(groupName))
                            _accountManager.Groups[groupName] = new List<int>();
                        
                        var indices = content.Split('&')
                            .Select(s => int.TryParse(s, out int idx) ? idx - 1 : -1)
                            .Where(idx => idx >= 0 && idx < _accountManager.Accounts.Count).ToList();
                        _accountManager.Groups[groupName].AddRange(indices);
                    }
                }
                
                _accounts.Clear();
                foreach (var acc in _accountManager.Accounts)
                {
                    _accounts.Add(new GameAccount 
                    { 
                        Username = acc.Account, 
                        Password = acc.Password, 
                        Server = $"S{acc.Server}", 
                        Status = "未登录",
                        Remark = acc.Remark
                    });
                }
                
                UpdateEmptyHint();
                SaveAccountList();
                SwitchGroup(_currentGroup >= 0 ? _currentGroup : 1);
                
                string actionText = action switch
                {
                    ImportAction.Overwrite => "覆盖重复账号后",
                    ImportAction.SkipDuplicates => "跳过重复账号后",
                    _ => ""
                };
                BottomStatus.Text = $"✅ {actionText}成功导入 {_accountManager.Accounts.Count} 个账号";
            }
            else if (!string.IsNullOrWhiteSpace(dialog.Username))
            {
                // 单个添加
                var account = new GameAccount 
                { 
                    Username = dialog.Username, 
                    Password = dialog.Password, 
                    Server = dialog.Server, 
                    Status = "未登录",
                    Remark = dialog.Remark
                };
                
                int newIndex = _accountManager.Accounts.Count;
                _accountManager.AddAccount(new AccountInfo36
                {
                    Platform = "4399",
                    Server = dialog.Server.Replace("S", "").Replace("s", ""),
                    Nickname = dialog.Username,
                    Account = dialog.Username,
                    Password = dialog.Password,
                    Remark = dialog.Remark
                });
                
                string groupName = dialog.Group;
                if (!_accountManager.Groups.ContainsKey(groupName))
                    _accountManager.Groups[groupName] = new List<int>();
                _accountManager.Groups[groupName].Add(newIndex);
                
                SaveAccountList();
                
                // 判断是否需要添加到当前显示列表
                string[] cnNums = ["", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十"];
                if (_currentGroup == -1)
                {
                    // 显示全部账号
                    _accounts.Add(account);
                }
                else
                {
                    string currentGroupName = $"第{cnNums[_currentGroup]}小组";
                    if (currentGroupName == groupName)
                    {
                        _accounts.Add(account);
                    }
                }
                
                UpdateEmptyHint();
                BottomStatus.Text = $"✅ {account.Username} 已添加到 {groupName}";
            }
        }
    }
    
    private async void Account_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is GameAccount account)
        {
            if (account.IsRunning)
            {
                System.Windows.MessageBox.Show("该账号已在运行中，请先停止游戏后再登录", "提示", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            account.Status = "登录中...";
            BottomStatus.Text = $"🔄 {account.Username} 正在登录...";
            
            try
            {
                var loginService = new Login4399Service();
                var result = await loginService.LoginAsync(account.Username, account.Password);
                
                while (result.NeedCaptcha && result.CaptchaImage != null)
                {
                    account.Status = "输入验证码...";
                    var captchaWindow = new CaptchaWindow(result.CaptchaImage);
                    captchaWindow.Owner = this;
                    if (captchaWindow.ShowDialog() == true)
                    {
                        result = await loginService.LoginAsync(account.Username, account.Password, captchaWindow.CaptchaCode);
                    }
                    else 
                    { 
                        account.Status = "登录取消"; 
                        BottomStatus.Text = $"⏹️ {account.Username} 登录取消";
                        return; 
                    }
                }
                
                if (!result.Success) 
                {
                    account.Status = "登录失败";
                    System.Windows.MessageBox.Show(result.ErrorMessage, "登录失败", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    BottomStatus.Text = $"❌ {account.Username} 登录失败";
                    return; 
                }
                
                string serverNum = account.Server.Replace("S", "").Replace("s", "");
                string gameUrl = await loginService.GetGameUrlAsync(account.Username, serverNum, result.Cookies);
                string title = $"[4399-{serverNum}-{account.Username}]|1|{account.Username}|0|0|0";
                
                FlashGameHost.ActivateFlashContext();
                
                var t = new Thread(() => 
                {
                    FlashGameHost.Run(title, gameUrl);
                    Dispatcher.Invoke(() => 
                    {
                        account.IsRunning = false;
                        account.Status = "未登录";
                    });
                });
                t.SetApartmentState(ApartmentState.STA);
                t.IsBackground = true;
                t.Start();
                
                account.IsRunning = true;
                account.Status = "运行中";
                BottomStatus.Text = $"✅ {account.Username} 游戏已启动";
            }
            catch (Exception ex)
            {
                account.Status = "错误";
                System.Windows.MessageBox.Show($"登录异常: {ex.Message}", "登录错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                BottomStatus.Text = $"⚠️ {account.Username} 登录异常";
            }
        }
    }
    
    private void StopAccount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string username)
        {
            var account = _accounts.FirstOrDefault(a => a.Username == username);
            if (account != null && account.IsRunning)
            {
                FlashGameHost.Stop();
                account.IsRunning = false;
                account.Status = "未登录";
                BottomStatus.Text = $"⏹️ {account.Username} 游戏已停止";
            }
        }
    }
    
    private void DeleteAccount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is string username)
        {
            var account = _accounts.FirstOrDefault(a => a.Username == username);
            if (account != null)
            {
                int managerIndex = _accountManager.Accounts.FindIndex(a => a.Account == username);
                if (managerIndex >= 0)
                    _accountManager.RemoveAccount(managerIndex);
                
                _accounts.Remove(account);
                UpdateEmptyHint();
                SaveAccountList();
                BottomStatus.Text = $"🗑️ 已删除 {username}";
            }
        }
    }
    
    private void EditAccount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is string username)
        {
            var account = _accounts.FirstOrDefault(a => a.Username == username);
            if (account != null)
            {
                var managerAccount = _accountManager.Accounts.FirstOrDefault(a => a.Account == username);
                string group = "第1小组";
                if (managerAccount != null)
                {
                    foreach (var g in _accountManager.Groups)
                    {
                        int idx = _accountManager.Accounts.FindIndex(a => a.Account == username);
                        if (g.Value.Contains(idx))
                        {
                            group = g.Key;
                            break;
                        }
                    }
                }
                
                var dialog = new AddAccountWindow(account.Username, account.Password, 
                    account.Server, group, account.Remark);
                dialog.Owner = this;
                if (dialog.ShowDialog() == true)
                {
                    account.Username = dialog.Username;
                    account.Password = dialog.Password;
                    account.Server = dialog.Server;
                    account.Remark = dialog.Remark;
                    
                    if (managerAccount != null)
                    {
                        managerAccount.Account = dialog.Username;
                        managerAccount.Password = dialog.Password;
                        managerAccount.Server = dialog.Server.Replace("S", "").Replace("s", "");
                        managerAccount.Remark = dialog.Remark;
                    }
                    
                    SaveAccountList();
                    BottomStatus.Text = $"✅ {dialog.Username} 已修改";
                }
            }
        }
    }
    
    private void Refresh_Click(object sender, RoutedEventArgs e) 
    { 
        _accounts.Clear(); 
        LoadSavedAccounts(); 
        UpdateEmptyHint(); 
    }
    
    private void Group1_Click(object sender, RoutedEventArgs e) => ToggleGroup(1, sender as System.Windows.Controls.Button);
    private void Group2_Click(object sender, RoutedEventArgs e) => ToggleGroup(2, sender as System.Windows.Controls.Button);
    private void Group3_Click(object sender, RoutedEventArgs e) => ToggleGroup(3, sender as System.Windows.Controls.Button);
    private void Group4_Click(object sender, RoutedEventArgs e) => ToggleGroup(4, sender as System.Windows.Controls.Button);
    private void Group5_Click(object sender, RoutedEventArgs e) => ToggleGroup(5, sender as System.Windows.Controls.Button);
    private void Group6_Click(object sender, RoutedEventArgs e) => ToggleGroup(6, sender as System.Windows.Controls.Button);
    private void Group7_Click(object sender, RoutedEventArgs e) => ToggleGroup(7, sender as System.Windows.Controls.Button);
    private void Group8_Click(object sender, RoutedEventArgs e) => ToggleGroup(8, sender as System.Windows.Controls.Button);
    private void Group9_Click(object sender, RoutedEventArgs e) => ToggleGroup(9, sender as System.Windows.Controls.Button);
    private void Group10_Click(object sender, RoutedEventArgs e) => ToggleGroup(10, sender as System.Windows.Controls.Button);
    
    private void ToggleGroup(int groupNum, System.Windows.Controls.Button? clickedButton)
    {
        if (_currentGroup == groupNum && _selectedButton != null)
        {
            _selectedButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x9C, 0x27, 0xB0));
            _selectedButton = null;
            _currentGroup = -1;
            ShowAllAccounts();
            BottomStatus.Text = "📁 显示全部账号";
            return;
        }
        
        if (_selectedButton != null)
            _selectedButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x9C, 0x27, 0xB0));
        
        if (clickedButton != null)
        {
            clickedButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50));
            _selectedButton = clickedButton;
        }
        
        SwitchGroup(groupNum);
    }
    
    private void SwitchGroup(int groupNum)
    {
        _currentGroup = groupNum;
        string[] cnNums = ["", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十"];
        string groupName = $"第{cnNums[groupNum]}小组";
        
        _accounts.Clear();
        
        if (_accountManager.Groups.TryGetValue(groupName, out var indices))
        {
            foreach (var idx in indices)
            {
                if (idx >= 0 && idx < _accountManager.Accounts.Count)
                {
                    var acc = _accountManager.Accounts[idx];
                    _accounts.Add(new GameAccount 
                    { 
                        Username = acc.Account, 
                        Password = acc.Password, 
                        Server = $"S{acc.Server}", 
                        Status = "未登录",
                        Remark = acc.Remark
                    });
                }
            }
        }
        
        UpdateEmptyHint();
        BottomStatus.Text = $"📁 {groupName} ({_accounts.Count}个账号)";
        
        // 调试日志
        string logFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "switch_debug.log");
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"切换到: {groupName}");
        sb.AppendLine($"Groups数量: {_accountManager.Groups.Count}");
        foreach (var g in _accountManager.Groups)
            sb.AppendLine($"  {g.Key}: [{string.Join(",", g.Value)}]");
        sb.AppendLine($"_accounts数量: {_accounts.Count}");
        foreach (var acc in _accounts)
            sb.AppendLine($"  {acc.Username}");
        System.IO.File.WriteAllText(logFile, sb.ToString());
    }
    
    private void ShowAllAccounts()
    {
        _accounts.Clear();
        foreach (var acc in _accountManager.Accounts)
        {
            _accounts.Add(new GameAccount 
            { 
                Username = acc.Account, 
                Password = acc.Password, 
                Server = $"S{acc.Server}", 
                Status = "未登录",
                Remark = acc.Remark
            });
        }
        UpdateEmptyHint();
    }
    
    private void UpdateEmptyHint() 
    { 
        EmptyHint.Visibility = _accounts.Count == 0 ? Visibility.Visible : Visibility.Collapsed; 
    }
    
    private void SaveAccountList()
    {
        try
        {
            _accountManager.SaveAccounts();
            _accountManager.SaveGroups();
        }
        catch { }
    }
    
    private void LoadSavedAccounts()
    {
        try
        {
            _accountManager.LoadAccounts();
            _accountManager.LoadGroups();
        }
        catch { }
    }
    
    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var account in _accounts)
        {
            account.IsSelected = true;
        }
        UpdateSelectedCount();
        BottomStatus.Text = "已全选";
    }
    
    private void SelectNone_Click(object sender, RoutedEventArgs e)
    {
        foreach (var account in _accounts)
        {
            account.IsSelected = false;
        }
        UpdateSelectedCount();
        BottomStatus.Text = "已取消全选";
    }
    
    private void BatchDelete_Click(object sender, RoutedEventArgs e)
    {
        var selectedAccounts = _accounts.Where(a => a.IsSelected).ToList();
        if (selectedAccounts.Count == 0)
        {
            BottomStatus.Text = "⚠️ 请先选择要删除的账号";
            return;
        }
        
        var result = System.Windows.MessageBox.Show(
            $"确定要删除选中的 {selectedAccounts.Count} 个账号吗？", 
            "批量删除", 
            MessageBoxButton.YesNo, 
            MessageBoxImage.Warning);
        
        if (result == MessageBoxResult.Yes)
        {
            foreach (var account in selectedAccounts)
            {
                int managerIndex = _accountManager.Accounts.FindIndex(a => a.Account == account.Username);
                if (managerIndex >= 0)
                    _accountManager.RemoveAccount(managerIndex);
                _accounts.Remove(account);
            }
            
            UpdateEmptyHint();
            SaveAccountList();
            UpdateSelectedCount();
            BottomStatus.Text = $"🗑️ 已删除 {selectedAccounts.Count} 个账号";
        }
    }
    
    private void UpdateSelectedCount()
    {
        int selectedCount = _accounts.Count(a => a.IsSelected);
        SelectedCount.Text = $"已选择 {selectedCount} 个";
    }
}
