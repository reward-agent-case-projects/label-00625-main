namespace PluginSystem.Core;

/// <summary>
/// 插件热重载服务 - 监控插件变化并自动重载
/// </summary>
public class PluginReloadService : IHostedService, IDisposable
{
    private readonly PluginManager _pluginManager;
    private readonly PluginServiceProvider _serviceProvider;
    private readonly PluginOptions _options;
    private readonly ILogger<PluginReloadService> _logger;
    private readonly IServiceProvider _appServiceProvider;
    private PluginWatcher? _watcher;

    public PluginReloadService(
        PluginManager pluginManager,
        PluginServiceProvider serviceProvider,
        PluginOptions options,
        ILogger<PluginReloadService> logger,
        IServiceProvider appServiceProvider)
    {
        _pluginManager = pluginManager;
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
        _appServiceProvider = appServiceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.EnableHotReload)
        {
            _logger.LogInformation("插件热重载服务已禁用");
            return Task.CompletedTask;
        }

        var pluginsPath = _pluginManager.PluginsPath;
        if (string.IsNullOrEmpty(pluginsPath))
        {
            pluginsPath = Path.Combine(AppContext.BaseDirectory, _options.PluginPath);
        }

        var watcherLogger = _appServiceProvider.GetRequiredService<ILogger<PluginWatcher>>();
        _watcher = new PluginWatcher(pluginsPath, _options, watcherLogger);
        _watcher.PluginChanged += OnPluginChanged;
        _watcher.Start();

        _logger.LogInformation("插件热重载服务已启动，监控目录: {Path}", pluginsPath);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _watcher?.Stop();
        _logger.LogInformation("插件热重载服务已停止");
        return Task.CompletedTask;
    }

    private void OnPluginChanged(object? sender, PluginChangedEventArgs e)
    {
        _logger.LogInformation("检测到插件变化: {PluginName}, 类型: {ChangeType}", 
            e.PluginName, e.ChangeType);

        try
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    HandlePluginCreated(e);
                    break;
                case WatcherChangeTypes.Changed:
                    HandlePluginChanged(e);
                    break;
                case WatcherChangeTypes.Deleted:
                    HandlePluginDeleted(e);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理插件变化失败: {PluginName}", e.PluginName);
        }
    }

    private void HandlePluginCreated(PluginChangedEventArgs e)
    {
        _logger.LogInformation("正在加载新插件: {PluginName}", e.PluginName);
        
        var plugin = _pluginManager.LoadPlugin(e.PluginPath);
        if (plugin != null)
        {
            var config = LoadPluginConfiguration(e.PluginPath);
            _serviceProvider.RegisterPlugin(plugin, config);
            _logger.LogInformation("新插件已加载: {PluginName}", e.PluginName);
        }
    }

    private void HandlePluginChanged(PluginChangedEventArgs e)
    {
        _logger.LogInformation("正在热重载插件服务: {PluginName}", e.PluginName);
        
        var plugin = _pluginManager.GetPlugin(e.PluginName);
        if (plugin != null)
        {
            var config = LoadPluginConfiguration(e.PluginPath);
            _serviceProvider.ReloadPluginServices(e.PluginName, plugin, config);
            _logger.LogInformation("插件服务热重载完成: {PluginName}", e.PluginName);
        }
    }

    private void HandlePluginDeleted(PluginChangedEventArgs e)
    {
        _logger.LogInformation("正在卸载插件: {PluginName}", e.PluginName);
        
        _serviceProvider.UnloadPlugin(e.PluginName);
        _pluginManager.UnloadPlugin(e.PluginName);
        
        _logger.LogInformation("插件已卸载: {PluginName}", e.PluginName);
    }

    private IConfiguration LoadPluginConfiguration(string pluginPath)
    {
        var configBuilder = new ConfigurationBuilder();
        var configFile = Path.Combine(pluginPath, "appsettings.json");
        if (File.Exists(configFile))
        {
            configBuilder.AddJsonFile(configFile, optional: true, reloadOnChange: true);
        }
        return configBuilder.Build();
    }

    public void Dispose()
    {
        _watcher?.Dispose();
    }
}
