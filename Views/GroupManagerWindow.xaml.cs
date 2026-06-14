using System.Collections.Generic;
using System.Windows;

namespace DDTankLauncher.Views
{
    public partial class GroupManagerWindow : Window
    {
        private readonly Dictionary<string, List<int>> _groups;
        
        public Dictionary<string, List<int>> Groups => _groups;
        public bool HasChanges { get; private set; }
        
        public GroupManagerWindow(Dictionary<string, List<int>> groups)
        {
            InitializeComponent();
            _groups = groups;
            RefreshGroupList();
        }
        
        private void RefreshGroupList()
        {
            GroupList.Items.Clear();
            foreach (var group in _groups)
            {
                GroupList.Items.Add($"{group.Key} ({group.Value.Count}个账号)");
            }
        }
        
        private void AddGroup_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "新建分组",
                FileName = "新分组",
                DefaultExt = ".txt",
                Filter = "文本文件|*.txt"
            };
            
            if (dialog.ShowDialog() == true)
            {
                string groupName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                if (!string.IsNullOrEmpty(groupName) && !_groups.ContainsKey(groupName))
                {
                    _groups[groupName] = new List<int>();
                    RefreshGroupList();
                    HasChanges = true;
                }
            }
        }
        
        private void RenameGroup_Click(object sender, RoutedEventArgs e)
        {
            if (GroupList.SelectedIndex < 0) return;
            
            string oldName = GetSelectedGroupName();
            if (string.IsNullOrEmpty(oldName)) return;
            
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "重命名分组",
                FileName = oldName,
                DefaultExt = ".txt",
                Filter = "文本文件|*.txt"
            };
            
            if (dialog.ShowDialog() == true)
            {
                string newName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                if (!string.IsNullOrEmpty(newName) && newName != oldName && !_groups.ContainsKey(newName))
                {
                    var accounts = _groups[oldName];
                    _groups.Remove(oldName);
                    _groups[newName] = accounts;
                    RefreshGroupList();
                    HasChanges = true;
                }
            }
        }
        
        private void DeleteGroup_Click(object sender, RoutedEventArgs e)
        {
            if (GroupList.SelectedIndex < 0) return;
            
            string groupName = GetSelectedGroupName();
            if (string.IsNullOrEmpty(groupName)) return;
            
            if (System.Windows.MessageBox.Show($"确定删除分组 \"{groupName}\"？", "确认", 
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _groups.Remove(groupName);
                RefreshGroupList();
                HasChanges = true;
            }
        }
        
        private string GetSelectedGroupName()
        {
            if (GroupList.SelectedItem is string item)
            {
                // 格式: "分组名 (X个账号)"
                int index = item.IndexOf(" (");
                if (index > 0)
                    return item.Substring(0, index);
            }
            return "";
        }
        
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
