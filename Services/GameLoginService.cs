using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace DDTankLauncher.Services;

/// <summary>
/// 游戏登录服务 - 处理4399登录流程
/// </summary>
public class GameLoginService
{
    private readonly HttpClient _httpClient;
    
    // 服务器域名配置
    private const string LOGIN_URL = "http://assist.ddt.1322game.com/4399/22/-15/login";
    private const string GAME_SWF_PATTERN = "http://s{0}.ddt.1322game.com/Loading.swf";
    
    public GameLoginService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }
    
    /// <summary>
    /// 获取游戏登录链接
    /// </summary>
    public async Task<GameLoginResult> GetGameLoginUrlAsync(string username, string server)
    {
        try
        {
            // 提取服务器编号 (例如: S132 -> 132)
            string serverNum = server.Replace("S", "").Replace("s", "");
            
            // 构建登录URL
            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            string loginUrl = $"{LOGIN_URL}?username={username}&time={timestamp}&flag=1&cm=1&server={server}&device_type=web";
            
            // 请求登录页面
            var response = await _httpClient.GetAsync(loginUrl);
            string html = await response.Content.ReadAsStringAsync();
            
            // 解析 SWF URL
            string swfUrl = string.Format(GAME_SWF_PATTERN, serverNum);
            
            // 尝试从响应中提取更精确的 SWF URL
            var swfMatch = Regex.Match(html, @"(http[s]?://[^""']*\.swf[^""']*)", RegexOptions.IgnoreCase);
            if (swfMatch.Success)
            {
                swfUrl = swfMatch.Groups[1].Value;
            }
            
            return new GameLoginResult
            {
                Success = true,
                SwfUrl = swfUrl,
                Server = server,
                Username = username
            };
        }
        catch (Exception ex)
        {
            return new GameLoginResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
    
    /// <summary>
    /// 下载 SWF 文件到本地
    /// </summary>
    public async Task<string> DownloadSwfAsync(string swfUrl, string savePath)
    {
        try
        {
            var response = await _httpClient.GetAsync(swfUrl);
            response.EnsureSuccessStatusCode();
            
            byte[] swfBytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(savePath, swfBytes);
            
            return savePath;
        }
        catch (Exception ex)
        {
            throw new Exception($"下载 SWF 失败: {ex.Message}");
        }
    }
}

/// <summary>
/// 游戏登录结果
/// </summary>
public class GameLoginResult
{
    public bool Success { get; set; }
    public string SwfUrl { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
