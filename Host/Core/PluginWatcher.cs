namespace PluginSystem.Core;

/// <summary>
/// 插件文件监控器 - 监控插件目录变化，实现热重载
/// </summary>
public class PluginWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly ILogger<PluginWatcher> _logger;
    private readonly PluginOptions _options;
    private readonly CancellationTokenSource _cts = new();
    private DateTime _lastReloadTime = DateTime.MinValue;
    private readonly object _lock = new();

    public event EventHandler<PluginChangedEventArgs>? PluginChanged;

    public PluginWatcher(
        string pluginsPath,
        PluginOptions options,
        ILogger<PluginWatcher> logger)
    {
        _options = options;
        _logger = logger;

        if (!Directory.Exists(pluginsPath))
        {
            Directory.CreateDirectory(pluginsPath);
        }

        _watcher = new FileSystemWatcher(pluginsPath)
        {
            NotifyFilter = NotifyFilters.LastWrite 
                         | NotifyFilters.FileName 
                         | NotifyFilters.DirectoryName,
            IncludeSubdirectories = true,
            EnableRaisingEvents = false
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Deleted += OnFileChanged;
        _watcher.Renamed += OnFileRenamed;
    }

    /// <summary>
    /// 开始监控
    /// </summary>
    public void Start()
    {
        if (!_options.EnableHotReload)
        {
            _logger.LogInformation("插件热重载已禁用");
            return;
        }

        _watcher.EnableRaisingEvents = true;
        _logger.LogInformation("插件热重载监控已启动，目录: {Path}", _watcher.Path);
    }

    /// <summary>
    /// 停止监控
    /// </summary>
    public void Stop()
    {
        _watcher.EnableRaisingEvents = false;
        _logger.LogInformation("插件热重载监控已停止");
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // 只关注 DLL 文件变化
        if (!e.FullPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            return;

        HandleChange(e.FullPath, e.ChangeType);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (!e.FullPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            return;

        HandleChange(e.FullPath, e.ChangeType);
    }

    private void HandleChange(string fullPath, WatcherChangeTypes changeType)
    {
        lock (_lock)
        {
            // 防抖：避免短时间内多次触发
            var now = DateTime.UtcNow;
            if ((now - _lastReloadTime).TotalMilliseconds < _options.HotReloadDelay)
                return;

            _lastReloadTime = now;
        }

        // 获取插件名称（目录名）
        var pluginDir = Path.GetDirectoryName(fullPath);
        var pluginName = Path.GetFileName(pluginDir);

        _logger.LogInformation("检测到插件变化: {PluginName}, 类型: {ChangeType}", pluginName, changeType);

        // 延迟触发，等待文件写入完成
        Task.Delay(_options.HotReloadDelay, _cts.Token).ContinueWith(_ =>
        {
            PluginChanged?.Invoke(this, new PluginChangedEventArgs
            {
                PluginName = pluginName ?? string.Empty,
                PluginPath = pluginDir ?? string.Empty,
                ChangeType = changeType
            });
        }, TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _watcher.Dispose();
        _cts.Dispose();
    }
}

/// <summary>
/// 插件变化事件参数
/// </summary>
public class PluginChangedEventArgs : EventArgs
{
    public string PluginName { get; set; } = string.Empty;
    public string PluginPath { get; set; } = string.Empty;
    public WatcherChangeTypes ChangeType { get; set; }
}
