using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace DDTankLauncher.Views
{
    public partial class AddAccountWindow : Window
    {
        public string Username { get; private set; } = "";
        public string Password { get; private set; } = "";
        public string Server { get; private set; } = "";
        public string Group { get; private set; } = "第1小组";
        public string Remark { get; private set; } = "";
        public List<string[]> ImportedAccounts { get; private set; } = new();
        public bool IsImportMode { get; private set; }
        
        public AddAccountWindow()
        {
            InitializeComponent();
        }

        public AddAccountWindow(string username, string password, string server, string group, string remark)
        {
            InitializeComponent();
            Title = "编辑账号";
            UsernameBox.Text = username;
            PasswordBox.Password = password;
            ServerBox.Text = server;
            RemarkBox.Text = remark;

            foreach (System.Windows.Controls.ComboBoxItem item in GroupComboBox.Items)
            {
                if (item.Content?.ToString() == group)
                {
                    GroupComboBox.SelectedItem = item;
                    break;
                }
            }
        }
        
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UsernameBox.Text))
            {
                System.Windows.MessageBox.Show("请输入账号", "提示");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                System.Windows.MessageBox.Show("请输入密码", "提示");
                return;
            }
            
            Username = UsernameBox.Text.Trim();
            Password = PasswordBox.Password;
            Server = ServerBox.Text.Trim();
            Group = (GroupComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "第1小组";
            Remark = RemarkBox.Text.Trim();
            
            DialogResult = true;
        }
        
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
        
        /// <summary>
        /// 一键导入36脚本大厅账号
        /// </summary>
        private void Import36_Click(object sender, RoutedEventArgs e)
        {
            string dataDir = @"C:\1210107875\36JB_COM_CONFIG\data";
            string userdataFile = Path.Combine(dataDir, "userdata");
            
            if (!File.Exists(userdataFile))
            {
                System.Windows.MessageBox.Show($"未找到36脚本大厅账号文件\n路径: {userdataFile}", "导入失败");
                return;
            }
            
            try
            {
                var lines = File.ReadAllLines(userdataFile, Encoding.GetEncoding("gb2312"));
                int count = 0;
                
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    var parts = line.Split('$');
                    if (parts.Length >= 7)
                    {
                        string platform = parts[0];
                        string server = parts[1];
                        string nickname = parts[2];
                        string account = parts[3];
                        string field4 = parts[4];
                        string field5 = parts[5];
                        string passwordEnc = parts[6];
                        
                        // 解密密码（暂时用密文）
                        string password = passwordEnc; // TODO: 解密
                        
                        ImportedAccounts.Add(new string[] { 
                            platform, server, nickname, account, password, field4, field5 
                        });
                        count++;
                    }
                }
                
                if (count > 0)
                {
                    IsImportMode = true;
                    System.Windows.MessageBox.Show("导入成功", "导入成功");
                    DialogResult = true;
                }
                else
                {
                    System.Windows.MessageBox.Show("未找到账号数据", "导入失败");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"导入失败: {ex.Message}", "错误");
            }
        }
    }
}
