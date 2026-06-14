using System.IO;
using System.Net.Http;

namespace DDTankLauncher.Helpers;

/// <summary>
/// 资源管理器 - 管理游戏资源文件
/// </summary>
public static class ResourceManager
{
    private static readonly string AppDir = AppDomain.CurrentDomain.BaseDirectory;
    private static readonly string ResourcesDir = Path.Combine(AppDir, "Resources");
    private static readonly string CacheDir = Path.Combine(AppDir, "Cache");
    
    /// <summary>
    /// 确保目录存在
    /// </summary>
    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(ResourcesDir);
        Directory.CreateDirectory(CacheDir);
    }
    
    /// <summary>
    /// 获取 Flash Player 路径
    /// </summary>
    public static string GetFlashPlayerPath()
    {
        return Path.Combine(ResourcesDir, "flashplayer_sa.exe");
    }
    
    /// <summary>
    /// 获取缓存的 SWF 路径
    /// </summary>
    public static string GetCachedSwfPath(string server)
    {
        return Path.Combine(CacheDir, $"Loading_{server}.swf");
    }
    
    /// <summary>
    /// 检查 Flash Player 是否存在
    /// </summary>
    public static bool FlashPlayerExists()
    {
        return File.Exists(GetFlashPlayerPath());
    }
    
    /// <summary>
    /// 下载文件
    /// </summary>
    public static async Task DownloadFileAsync(string url, string savePath, IProgress<double>? progress = null)
    {
        using var httpClient = new HttpClient();
        
        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        
        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        var totalBytesRead = 0L;
        
        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
        
        byte[] buffer = new byte[8192];
        int bytesRead;
        
        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead);
            
            totalBytesRead += bytesRead;
            
            if (totalBytes > 0)
            {
                progress?.Report((double)totalBytesRead / totalBytes * 100);
            }
        }
    }
    
    /// <summary>
    /// 清理缓存
    /// </summary>
    public static void ClearCache()
    {
        if (Directory.Exists(CacheDir))
        {
            Directory.Delete(CacheDir, true);
            Directory.CreateDirectory(CacheDir);
        }
    }
}
