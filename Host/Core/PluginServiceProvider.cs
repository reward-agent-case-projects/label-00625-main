using PluginSystem.Contracts;

namespace PluginSystem.Core;

/// <summary>
/// 插件服务提供器 - 支持运行时热重载的动态服务容器
/// 实现 IPluginServiceAccessor 接口，让控制器能动态获取服务
/// 
/// 架构说明：
/// - 每个插件拥有独立的 DI 容器（PluginContainer）
/// - 插件服务不注册到主容器，避免主容器不可变的限制
/// - 控制器通过 IPluginServiceAccessor 动态获取服务
/// - 热重载时只需重建插件容器，无需重启应用
/// </summary>
public class PluginServiceProvider : IPluginServiceAccessor, IDisposable
{
    private readonly object _lock = new();
    private readonly ILogger<PluginServiceProvider> _logger;
    private readonly Dictionary<string, PluginContainer> _containers = new();

    public PluginServiceProvider(ILogger<PluginServiceProvider> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 注册插件到独立容器
    /// 调用插件的 ConfigureServices 方法配置依赖注入
    /// </summary>
    public void RegisterPlugin(PluginInfo plugin, IConfiguration configuration)
    {
        lock (_lock)
        {
            // 如果已存在，先释放旧容器
            if (_containers.TryGetValue(plugin.Plugin.Name, out var oldContainer))
            {
                oldContainer.DisposeServices();
                _containers.Remove(plugin.Plugin.Name);
            }

            // 创建新的插件容器（内部会调用 ConfigureServices）
            var container = new PluginContainer(plugin, configuration, _logger);
            _containers[plugin.Plugin.Name] = container;
            
            _logger.LogInformation("插件容器已注册: {PluginName}", plugin.Plugin.Name);
        }
    }

    /// <summary>
    /// 热重载插件服务（重建服务容器，不卸载程序集）
    /// </summary>
    public void ReloadPluginServices(string pluginName, PluginInfo plugin, IConfiguration configuration)
    {
        lock (_lock)
        {
            // 释放旧的服务容器
            if (_containers.TryGetValue(pluginName, out var oldContainer))
            {
                oldContainer.DisposeServices();
                _containers.Remove(pluginName);
                _logger.LogInformation("旧插件服务容器已释放: {PluginName}", pluginName);
            }

            // 创建新的服务容器
            var newContainer = new PluginContainer(plugin, configuration, _logger);
            _containers[plugin.Plugin.Name] = newContainer;
            
            _logger.LogInformation("新插件服务容器已创建: {PluginName}", plugin.Plugin.Name);
        }
    }

    /// <summary>
    /// 卸载插件服务
    /// </summary>
    public void UnloadPlugin(string pluginName)
    {
        lock (_lock)
        {
            if (_containers.TryGetValue(pluginName, out var container))
            {
                container.DisposeServices();
                _containers.Remove(pluginName);
                _logger.LogInformation("插件容器已卸载: {PluginName}", pluginName);
            }
        }
    }

    #region IPluginServiceAccessor 实现

    /// <summary>
    /// 获取指定插件的服务
    /// </summary>
    public T? GetService<T>(string pluginName) where T : class
    {
        lock (_lock)
        {
            if (_containers.TryGetValue(pluginName, out var container))
            {
                return container.GetService<T>();
            }
            return null;
        }
    }

    /// <summary>
    /// 获取指定插件的必需服务（不存在时抛出异常）
    /// </summary>
    public T GetRequiredService<T>(string pluginName) where T : class
    {
        var service = GetService<T>(pluginName);
        if (service == null)
        {
            throw new InvalidOperationException(
                $"无法从插件 '{pluginName}' 获取服务 '{typeof(T).Name}'");
        }
        return service;
    }

    /// <summary>
    /// 获取服务（自动查找所有插件）
    /// </summary>
    public T? GetService<T>() where T : class
    {
        lock (_lock)
        {
            foreach (var container in _containers.Values)
            {
                var service = container.GetService<T>();
                if (service != null)
                    return service;
            }
            return null;
        }
    }

    /// <summary>
    /// 获取必需服务（自动查找所有插件）
    /// </summary>
    public T GetRequiredService<T>() where T : class
    {
        var service = GetService<T>();
        if (service == null)
        {
            throw new InvalidOperationException(
                $"无法从任何插件获取服务 '{typeof(T).Name}'");
        }
        return service;
    }

    #endregion

    /// <summary>
    /// 获取插件服务（按类型查找）
    /// </summary>
    public object? GetService(Type serviceType)
    {
        lock (_lock)
        {
            foreach (var container in _containers.Values)
            {
                var service = container.GetService(serviceType);
                if (service != null)
                    return service;
            }
            return null;
        }
    }

    /// <summary>
    /// 获取所有插件名称
    /// </summary>
    public IReadOnlyList<string> GetPluginNames()
    {
        lock (_lock)
        {
            return _containers.Keys.ToList();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var container in _containers.Values)
            {
                container.DisposeServices();
            }
            _containers.Clear();
        }
    }
}

/// <summary>
/// 单个插件的服务容器
/// 为每个插件创建独立的 DI 容器，支持热重载
/// </summary>
public class PluginContainer
{
    private readonly PluginInfo _plugin;
    private IServiceProvider? _serviceProvider;
    private IServiceScope? _scope;
    private readonly ILogger _logger;
    private readonly object _lock = new();

    public PluginInfo Plugin => _plugin;

    public PluginContainer(PluginInfo plugin, IConfiguration configuration, ILogger logger)
    {
        _plugin = plugin;
        _logger = logger;
        BuildServices(configuration);
    }

    /// <summary>
    /// 构建服务容器
    /// 调用插件的 ConfigureServices 方法进行依赖注入配置
    /// </summary>
    private void BuildServices(IConfiguration configuration)
    {
        lock (_lock)
        {
            // 释放旧的
            DisposeServicesInternal();

            // 为插件创建独立的服务容器
            var services = new ServiceCollection();

            // 添加日志服务
            services.AddLogging(builder => builder.AddConsole());

            // 关键：调用插件的 ConfigureServices 方法
            // 这是依赖注入的核心，插件在此注册自己的服务
            _plugin.Plugin.ConfigureServices(services, configuration);
            _logger.LogDebug("已调用插件 {PluginName} 的 ConfigureServices 方法", _plugin.Plugin.Name);

            // 自动注册插件中标记的服务（IScopedService/ITransientService/ISingletonService）
            RegisterMarkedServices(services, _plugin);

            // 构建服务提供器
            _serviceProvider = services.BuildServiceProvider();
            _scope = _serviceProvider.CreateScope();
        }
    }

    /// <summary>
    /// 自动注册插件中标记的服务
    /// 扫描实现了 IScopedService/ITransientService/ISingletonService 接口的类型
    /// </summary>
    private void RegisterMarkedServices(IServiceCollection services, PluginInfo plugin)
    {
        var allTypes = plugin.Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface);

        foreach (var type in allTypes)
        {
            var interfaces = type.GetInterfaces();

            // 注册作用域服务
            if (interfaces.Contains(typeof(IScopedService)))
            {
                services.AddScoped(type);
                foreach (var iface in interfaces.Where(i => 
                    i != typeof(IScopedService) && 
                    !i.Namespace?.StartsWith("System") == true))
                {
                    services.AddScoped(iface, type);
                }
            }

            // 注册瞬时服务
            if (interfaces.Contains(typeof(ITransientService)))
            {
                services.AddTransient(type);
                foreach (var iface in interfaces.Where(i => 
                    i != typeof(ITransientService) && 
                    !i.Namespace?.StartsWith("System") == true))
                {
                    services.AddTransient(iface, type);
                }
            }

            // 注册单例服务
            if (interfaces.Contains(typeof(ISingletonService)))
            {
                services.AddSingleton(type);
                foreach (var iface in interfaces.Where(i => 
                    i != typeof(ISingletonService) && 
                    !i.Namespace?.StartsWith("System") == true))
                {
                    services.AddSingleton(iface, type);
                }
            }
        }
    }

    public T? GetService<T>() where T : class
    {
        lock (_lock)
        {
            return _scope?.ServiceProvider.GetService<T>();
        }
    }

    public object? GetService(Type serviceType)
    {
        lock (_lock)
        {
            return _scope?.ServiceProvider.GetService(serviceType);
        }
    }

    /// <summary>
    /// 释放服务（不卸载程序集）
    /// </summary>
    public void DisposeServices()
    {
        lock (_lock)
        {
            DisposeServicesInternal();
        }
    }

    private void DisposeServicesInternal()
    {
        _scope?.Dispose();
        _scope = null;

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        _serviceProvider = null;
    }
}
