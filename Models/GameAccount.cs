using System.ComponentModel;

namespace DDTankLauncher.Models;

/// <summary>
/// 游戏账号
/// </summary>
public class GameAccount : INotifyPropertyChanged
{
    private string _status = "未登录";
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _server = "S132";
    private string _cookies = string.Empty;
    private string _remark = string.Empty;
    private bool _isSelected = false;
    private bool _isRunning = false;
    
    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            _isRunning = value;
            OnPropertyChanged(nameof(IsRunning));
        }
    }
    
    public string Username
    {
        get => _username;
        set
        {
            _username = value;
            OnPropertyChanged(nameof(Username));
        }
    }
    
    public string Password
    {
        get => _password;
        set
        {
            _password = value;
            OnPropertyChanged(nameof(Password));
        }
    }
    
    public string Server
    {
        get => _server;
        set
        {
            _server = value;
            OnPropertyChanged(nameof(Server));
        }
    }
    
    public string Cookies
    {
        get => _cookies;
        set
        {
            _cookies = value;
            OnPropertyChanged(nameof(Cookies));
        }
    }
    
    public string Remark
    {
        get => _remark;
        set
        {
            _remark = value;
            OnPropertyChanged(nameof(Remark));
        }
    }
    
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
        }
    }
    
    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged(nameof(Status));
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
