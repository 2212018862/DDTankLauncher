using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DDTankLauncher.Services
{
    public class AccountManager36
    {
        private readonly string _dataDir;
        private List<AccountInfo36> _accounts = new();
        private Dictionary<string, List<int>> _groups = new();
        
        public List<AccountInfo36> Accounts => _accounts;
        public Dictionary<string, List<int>> Groups => _groups;
        
        public AccountManager36(string dataDir)
        {
            _dataDir = dataDir;
            Directory.CreateDirectory(dataDir);
        }
        
        public List<AccountInfo36> ReadFrom36jb(string sourceDir)
        {
            List<AccountInfo36> imported = new();
            string userdataFile = Path.Combine(sourceDir, "userdata");
            if (!File.Exists(userdataFile)) return imported;
            
            string rawData = File.ReadAllText(userdataFile, Encoding.GetEncoding("gb2312"));
            rawData = rawData.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = rawData.Split('\n');
            
            string pending = "";
            foreach (var line in lines)
            {
                string t = line.Trim();
                if (string.IsNullOrEmpty(t)) continue;
                
                pending += t;
                if (pending.Contains("Key") && pending.Split('$').Length >= 7)
                {
                    var p = pending.Split('$');
                    imported.Add(new AccountInfo36
                    {
                        Platform = p[0], Server = p[1], Remark = p[2],
                        Account = p[3], Field4 = p[4], Field5 = p[5],
                        PasswordEncrypted = p[6]
                    });
                    pending = "";
                }
            }
            return imported;
        }
        
        public void ImportFrom36jb(string sourceDir)
        {
            string userdataFile = Path.Combine(sourceDir, "userdata");
            if (!File.Exists(userdataFile)) return;
            
            int startIndex = _accounts.Count; // 记录起始索引，用于追加
            string rawData = File.ReadAllText(userdataFile, Encoding.GetEncoding("gb2312"));
            rawData = rawData.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = rawData.Split('\n');
            
            string pending = "";
            foreach (var line in lines)
            {
                string t = line.Trim();
                if (string.IsNullOrEmpty(t)) continue;
                
                pending += t;
                if (pending.Contains("Key") && pending.Split('$').Length >= 7)
                {
                    var p = pending.Split('$');
                    _accounts.Add(new AccountInfo36
                    {
                        Platform = p[0], Server = p[1], Remark = p[2],
                        Account = p[3], Field4 = p[4], Field5 = p[5],
                        PasswordEncrypted = p[6],
                        Password = p[6]
                    });
                    pending = "";
                }
            }
            
            var groupFiles = Directory.GetFiles(sourceDir, "*小组.txt");
            System.Diagnostics.Debug.WriteLine($"ImportFrom36jb: accounts={_accounts.Count}");
            System.Diagnostics.Debug.WriteLine($"ImportFrom36jb: groupFiles={groupFiles.Length}");
            foreach (var file in groupFiles)
            {
                string groupName = Path.GetFileNameWithoutExtension(file);
                string content = File.ReadAllText(file, Encoding.GetEncoding("gb2312")).Trim();
                if (!string.IsNullOrEmpty(content))
                {
                    if (!_groups.ContainsKey(groupName))
                        _groups[groupName] = new List<int>();
                    
                    var indices = content.Split('&')
                        .Select(s => int.TryParse(s, out int idx) ? idx - 1 + startIndex : -1)
                        .Where(idx => idx >= 0).ToList();
                    _groups[groupName].AddRange(indices);
                    System.Diagnostics.Debug.WriteLine($"[IMPORT] {groupName}: [{string.Join(",", indices.Select(i => i+1))}]");
                }
            }
            
            // 写入日志文件
            string logFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "import_debug.log");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"导入账号数: {_accounts.Count}");
            foreach (var acc in _accounts)
                sb.AppendLine($"  {acc.Nickname} ({acc.Account})");
            sb.AppendLine();
            sb.AppendLine("分组:");
            foreach (var g in _groups)
                sb.AppendLine($"  {g.Key}: [{string.Join(",", g.Value.Select(i => i+1))}] -> [{string.Join(",", g.Value)}]");
            System.IO.File.WriteAllText(logFile, sb.ToString());
        }
        
        public void LoadAccounts()
        {
            _accounts.Clear();
            string f = Path.Combine(_dataDir, "userdata");
            if (!File.Exists(f)) return;
            
            string rawData = File.ReadAllText(f, Encoding.GetEncoding("gb2312"));
            rawData = rawData.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = rawData.Split('\n');
            
            foreach (var line in lines)
            {
                string t = line.Trim();
                if (string.IsNullOrEmpty(t)) continue;
                var p = t.Split('$');
                if (p.Length >= 7)
                {
                    _accounts.Add(new AccountInfo36
                    {
                        Platform = p[0], Server = p[1], Remark = p[2],
                        Account = p[3], Field4 = p[4], Field5 = p[5],
                        PasswordEncrypted = p[6],
                        Password = p[6]
                    });
                }
            }
        }
        
        public void LoadGroups()
        {
            _groups.Clear();
            var groupFiles = Directory.GetFiles(_dataDir, "*小组.txt");
            foreach (var file in groupFiles)
            {
                string groupName = Path.GetFileNameWithoutExtension(file);
                string content = File.ReadAllText(file, Encoding.GetEncoding("gb2312")).Trim();
                if (!string.IsNullOrEmpty(content))
                {
                    var indices = content.Split('&')
                        .Select(s => int.TryParse(s, out int idx) ? idx - 1 : -1)
                        .Where(idx => idx >= 0).ToList();
                    _groups[groupName] = indices;
                    System.Diagnostics.Debug.WriteLine($"[IMPORT] {groupName}: [{string.Join(",", indices.Select(i => i+1))}]");
                }
            }
            
            // 写入日志文件
            string logFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "import_debug.log");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"导入账号数: {_accounts.Count}");
            foreach (var acc in _accounts)
                sb.AppendLine($"  {acc.Nickname} ({acc.Account})");
            sb.AppendLine();
            sb.AppendLine("分组:");
            foreach (var g in _groups)
                sb.AppendLine($"  {g.Key}: [{string.Join(",", g.Value.Select(i => i+1))}] -> [{string.Join(",", g.Value)}]");
            System.IO.File.WriteAllText(logFile, sb.ToString());
        }
        
        public void SaveAccounts()
        {
            var lines = _accounts.Select(a => a.ToFileString());
            File.WriteAllLines(Path.Combine(_dataDir, "userdata"), lines, Encoding.GetEncoding("gb2312"));
        }
        
        public void SaveGroups()
        {
            foreach (var group in _groups)
            {
                string content = string.Join("&", group.Value.Select(i => i + 1));
                File.WriteAllText(Path.Combine(_dataDir, $"{group.Key}.txt"), content, Encoding.GetEncoding("gb2312"));
            }
        }
        
        public void AddAccount(AccountInfo36 account) => _accounts.Add(account);
        
        public void RemoveAccount(int index)
        {
            if (index >= 0 && index < _accounts.Count)
            {
                _accounts.RemoveAt(index);
                foreach (var group in _groups.Values)
                {
                    group.RemoveAll(i => i == index);
                    for (int i = 0; i < group.Count; i++)
                        if (group[i] > index) group[i]--;
                }
            }
        }
        
        public List<AccountInfo36> GetGroupAccounts(string groupName)
        {
            if (_groups.TryGetValue(groupName, out var indices))
                return indices.Where(i => i >= 0 && i < _accounts.Count).Select(i => _accounts[i]).ToList();
            return new List<AccountInfo36>();
        }
    }
    
    public class AccountInfo36
    {
        public string Platform { get; set; } = "4399";
        public string Server { get; set; } = "";
        public string Nickname { get; set; } = "";
        public string Account { get; set; } = "";
        public string Field4 { get; set; } = "1";
        public string Field5 { get; set; } = "1";
        public string PasswordEncrypted { get; set; } = "";
        public string Password { get; set; } = "";
        public string Remark { get; set; } = "";
        
        public string ToFileString()
        {
            string password = string.IsNullOrEmpty(Password) ? PasswordEncrypted : Password;
            return $"{Platform}${Server}${Remark}${Account}${Field4}${Field5}${password}";
        }
        public override string ToString() => $"{Platform}-{Server} {Remark} ({Account})";
    }
}
