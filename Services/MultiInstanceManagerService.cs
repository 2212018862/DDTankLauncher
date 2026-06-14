using System.Collections.Concurrent;
using System.Diagnostics;

namespace DDTankLauncher.Services;

/// <summary>
/// 多开管理服务 - 管理多个游戏实例
/// </summary>
public class MultiInstanceManagerService : IDisposable
{
    private readonly ConcurrentDictionary<int, GameInstance> _instances = new();
    private int _nextInstanceId = 1;
    
    /// <summary>
    /// 游戏实例列表
    /// </summary>
    public ICollection<GameInstance> Instances => _instances.Values;
    
    /// <summary>
    /// 实例数量
    /// </summary>
    public int InstanceCount => _instances.Count;
    
    /// <summary>
    /// 创建新实例
    /// </summary>
    public GameInstance CreateInstance(string username, string server)
    {
        int instanceId = Interlocked.Increment(ref _nextInstanceId);
        
        var instance = new GameInstance
        {
            Id = instanceId,
            Username = username,
            Server = server,
            Status = InstanceStatus.Created,
            CreatedAt = DateTime.Now
        };
        
        _instances.TryAdd(instanceId, instance);
        
        return instance;
    }
    
    /// <summary>
    /// 启动实例
    /// </summary>
    public async Task<bool> StartInstanceAsync(int instanceId, string flashPlayerPath, string swfPath)
    {
        if (!_instances.TryGetValue(instanceId, out var instance))
        {
            return false;
        }
        
        try
        {
            instance.Status = InstanceStatus.Starting;
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = flashPlayerPath,
                    Arguments = $"\"{swfPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = false
                },
                EnableRaisingEvents = true
            };
            
            process.Exited += (sender, args) =>
            {
                instance.Status = InstanceStatus.Stopped;
                instance.ProcessId = null;
            };
            
            process.Start();
            
            instance.ProcessId = process.Id;
            instance.Process = process;
            instance.Status = InstanceStatus.Running;
            
            return await Task.FromResult(true);
        }
        catch
        {
            instance.Status = InstanceStatus.Error;
            return false;
        }
    }
    
    /// <summary>
    /// 停止实例
    /// </summary>
    public bool StopInstance(int instanceId)
    {
        if (!_instances.TryGetValue(instanceId, out var instance))
        {
            return false;
        }
        
        try
        {
            if (instance.Process != null && !instance.Process.HasExited)
            {
                instance.Process.Kill();
            }
            
            instance.Status = InstanceStatus.Stopped;
            instance.ProcessId = null;
            
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// 停止所有实例
    /// </summary>
    public void StopAllInstances()
    {
        foreach (var instance in _instances.Values)
        {
            StopInstance(instance.Id);
        }
    }
    
    /// <summary>
    /// 移除实例
    /// </summary>
    public bool RemoveInstance(int instanceId)
    {
        StopInstance(instanceId);
        return _instances.TryRemove(instanceId, out _);
    }
    
    public void Dispose()
    {
        StopAllInstances();
        
        foreach (var instance in _instances.Values)
        {
            instance.Process?.Dispose();
        }
        
        _instances.Clear();
    }
}

/// <summary>
/// 游戏实例
/// </summary>
public class GameInstance
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public InstanceStatus Status { get; set; }
    public int? ProcessId { get; set; }
    public Process? Process { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 实例状态
/// </summary>
public enum InstanceStatus
{
    Created,
    Starting,
    Running,
    Stopped,
    Error
}
