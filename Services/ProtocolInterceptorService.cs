using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DDTankLauncher.Services;

/// <summary>
/// 协议拦截服务 - 代理 HTTP/WebSocket 请求
/// </summary>
public class ProtocolInterceptorService : IDisposable
{
    private TcpListener? _listener;
    private readonly ConcurrentDictionary<string, DateTime> _requestLog = new();
    private bool _isRunning;
    
    public int Port { get; }
    public bool IsRunning => _isRunning;
    
    public event EventHandler<ProtocolEventArgs>? RequestIntercepted;
    
    public ProtocolInterceptorService(int port = 8888)
    {
        Port = port;
    }
    
    /// <summary>
    /// 启动代理监听
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning) return;
        
        _listener = new TcpListener(IPAddress.Loopback, Port);
        _listener.Start();
        _isRunning = true;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client, cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                // 监听器已关闭
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接受连接失败: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 处理客户端连接
    /// </summary>
    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using var clientStream = client.GetStream();
        
        // 读取请求
        byte[] buffer = new byte[8192];
        int bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
        
        if (bytesRead == 0) return;
        
        string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);
        
        // 解析请求
        var lines = request.Split(new[] { "\r\n" }, StringSplitOptions.None);
        if (lines.Length == 0) return;
        
        // 提取请求行
        string requestLine = lines[0];
        string[] parts = requestLine.Split(' ');
        if (parts.Length < 3) return;
        
        string method = parts[0];
        string url = parts[1];
        
        // 触发拦截事件
        var args = new ProtocolEventArgs
        {
            Method = method,
            Url = url,
            Headers = ParseHeaders(lines),
            Body = ExtractBody(request),
            Timestamp = DateTime.Now
        };
        
        RequestIntercepted?.Invoke(this, args);
        
        // 记录请求
        _requestLog.TryAdd($"{method} {url}", DateTime.Now);
        
        // TODO: 实际的代理转发逻辑
        // 这里可以修改请求/响应，实现协议解析
        
        await clientStream.FlushAsync(cancellationToken);
    }
    
    /// <summary>
    /// 解析请求头
    /// </summary>
    private Dictionary<string, string> ParseHeaders(string[] lines)
    {
        var headers = new Dictionary<string, string>();
        
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrEmpty(lines[i])) break;
            
            int colonIndex = lines[i].IndexOf(':');
            if (colonIndex > 0)
            {
                string key = lines[i].Substring(0, colonIndex).Trim();
                string value = lines[i].Substring(colonIndex + 1).Trim();
                headers[key] = value;
            }
        }
        
        return headers;
    }
    
    /// <summary>
    /// 提取请求体
    /// </summary>
    private string ExtractBody(string request)
    {
        int headerEnd = request.IndexOf("\r\n\r\n");
        if (headerEnd > 0)
        {
            return request.Substring(headerEnd + 4);
        }
        return string.Empty;
    }
    
    /// <summary>
    /// 获取请求日志
    /// </summary>
    public IReadOnlyDictionary<string, DateTime> GetRequestLog()
    {
        return _requestLog;
    }
    
    public void Dispose()
    {
        _isRunning = false;
        _listener?.Stop();
        _listener?.Dispose();
    }
}

/// <summary>
/// 协议拦截事件参数
/// </summary>
public class ProtocolEventArgs : EventArgs
{
    public string Method { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Body { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
