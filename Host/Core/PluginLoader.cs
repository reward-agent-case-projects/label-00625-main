using PluginSystem.Contracts;

namespace PluginSystem.Core;

/// <summary>
/// 插件加载上下文 - 使用 AssemblyLoadContext 实现插件隔离
/// 每个插件拥有独立的加载上下文，支持热重载时的程序集卸载
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // 尝试从插件目录加载
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        // 尝试从共享依赖加载（避免重复加载共享程序集）
        var sharedAssembly = Default.Assemblies
            .FirstOrDefault(a => a.GetName().Name == assemblyName.Name);

        if (sharedAssembly != null)
        {
            return sharedAssembly;
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}

/// <summary>
/// 插件信息 - 封装已加载插件的所有相关信息
/// </summary>
public class PluginInfo
{
    /// <summary>插件程序集</summary>
    public Assembly Assembly { get; }
    
    /// <summary>插件加载上下文（用于卸载）</summary>
    public PluginLoadContext LoadContext { get; }
    
    /// <summary>插件实例</summary>
    public IPlugin Plugin { get; }
    
    /// <summary>插件中的控制器类型列表</summary>
    public List<Type> ControllerTypes { get; }
    
    /// <summary>插件中的模块初始化器类型列表</summary>
    public List<Type> InitializerTypes { get; }
    
    /// <summary>插件目录路径</summary>
    public string PluginPath { get; }
    
    /// <summary>插件配置</summary>
    public IConfiguration Configuration { get; }

    public PluginInfo(
        Assembly assembly,
        PluginLoadContext loadContext,
        IPlugin plugin,
        List<Type> controllerTypes,
        List<Type> initializerTypes,
        string pluginPath,
        IConfiguration configuration)
    {
        Assembly = assembly;
        LoadContext = loadContext;
        Plugin = plugin;
        ControllerTypes = controllerTypes;
        InitializerTypes = initializerTypes;
        PluginPath = pluginPath;
        Configuration = configuration;
    }
}

/// <summary>
/// 插件管理器 - 负责插件的加载、卸载、热重载和配置管理
/// </summary>
public class PluginManager
{
    private readonly object _lock = new();
    private readonly Dictionary<string, PluginInfo> _plugins = new();
    private readonly ILogger<PluginManager> _logger;
    private string _pluginsPath = string.Empty;

    public event EventHandler<PluginEventArgs>? PluginLoaded;
    public event EventHandler<PluginEventArgs>? PluginUnloaded;
    public event EventHandler<PluginEventArgs>? PluginReloaded;

    public PluginManager(ILogger<PluginManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 获取已加载的插件列表
    /// </summary>
    public IReadOnlyList<PluginInfo> Plugins
    {
        get
        {
            lock (_lock)
            {
                return _plugins.Values.ToList();
            }
        }
    }

    /// <summary>
    /// 插件目录路径
    /// </summary>
    public string PluginsPath => _pluginsPath;

    /// <summary>
    /// 加载所有插件
    /// </summary>
    public IReadOnlyList<PluginInfo> LoadPlugins(string pluginsPath)
    {
        _pluginsPath = pluginsPath;

        if (!Directory.Exists(pluginsPath))
        {
            Directory.CreateDirectory(pluginsPath);
            _logger.LogInformation("插件目录不存在，已创建: {PluginsPath}", pluginsPath);
            return Plugins;
        }

        _logger.LogInformation("开始加载插件，目录: {PluginsPath}", pluginsPath);

        var pluginDirectories = Directory.GetDirectories(pluginsPath);
        var loadedPlugins = new List<PluginInfo>();

        // 第一阶段：加载所有插件
        foreach (var pluginDir in pluginDirectories)
        {
            try
            {
                var plugin = LoadPlugin(pluginDir);
                if (plugin != null)
                {
                    loadedPlugins.Add(plugin);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载插件失败: {PluginDir}", pluginDir);
            }
        }

        // 第二阶段：按依赖关系排序
        loadedPlugins = SortByDependencies(loadedPlugins);

        // 重新组织字典顺序
        lock (_lock)
        {
            _plugins.Clear();
            foreach (var plugin in loadedPlugins)
            {
                _plugins[plugin.Plugin.Name] = plugin;
            }
        }

        _logger.LogInformation("插件加载完成，共 {Count} 个插件（已按依赖排序）", _plugins.Count);
        return Plugins;
    }

    /// <summary>
    /// 按依赖关系排序插件（拓扑排序）
    /// </summary>
    private List<PluginInfo> SortByDependencies(List<PluginInfo> plugins)
    {
        var sorted = new List<PluginInfo>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        void Visit(PluginInfo plugin)
        {
            if (visited.Contains(plugin.Plugin.Name))
                return;

            if (visiting.Contains(plugin.Plugin.Name))
            {
                _logger.LogWarning("检测到循环依赖: {PluginName}", plugin.Plugin.Name);
                return;
            }

            visiting.Add(plugin.Plugin.Name);

            // 先处理依赖
            foreach (var depName in plugin.Plugin.Dependencies)
            {
                var dep = plugins.FirstOrDefault(p => p.Plugin.Name == depName);
                if (dep != null)
                {
                    Visit(dep);
                }
            }

            visiting.Remove(plugin.Plugin.Name);
            visited.Add(plugin.Plugin.Name);
            sorted.Add(plugin);
        }

        foreach (var plugin in plugins)
        {
            Visit(plugin);
        }

        return sorted;
    }

    /// <summary>
    /// 加载单个插件
    /// </summary>
    public PluginInfo? LoadPlugin(string pluginPath)
    {
        var pluginName = Path.GetFileName(pluginPath);
        var dllName = $"{pluginName}.dll";
        var dllPath = Path.Combine(pluginPath, dllName);

        if (!File.Exists(dllPath))
        {
            _logger.LogWarning("插件DLL不存在: {DllPath}", dllPath);
            return null;
        }

        lock (_lock)
        {
            // 如果已存在，先卸载
            if (_plugins.ContainsKey(pluginName))
            {
                UnloadPlugin(pluginName);
            }

            // 1. 加载插件配置文件（ConfigurationBuilder + AddJsonFile）
            var configuration = LoadPluginConfiguration(pluginPath);
            _logger.LogDebug("已加载插件配置: {PluginPath}/appsettings.json", pluginPath);

            // 2. 创建插件加载上下文（AssemblyLoadContext 隔离）
            var loadContext = new PluginLoadContext(dllPath);

            // 3. 加载插件程序集
            var assembly = loadContext.LoadFromAssemblyPath(dllPath);

            // 4. 查找插件入口（实现 IPlugin 接口的类）
            var pluginType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract);

            if (pluginType == null)
            {
                _logger.LogWarning("未找到插件入口（需实现 IPlugin 接口）: {PluginPath}", pluginPath);
                loadContext.Unload();
                return null;
            }

            // 5. 创建插件实例
            if (Activator.CreateInstance(pluginType) is not IPlugin pluginInstance)
            {
                _logger.LogWarning("创建插件实例失败: {PluginPath}", pluginPath);
                loadContext.Unload();
                return null;
            }

            // 6. 查找控制器类型（命名约定：继承自 ControllerBase）
            var controllerTypes = assembly.GetTypes()
                .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
                .ToList();

            _logger.LogDebug("发现 {Count} 个控制器: {Controllers}", 
                controllerTypes.Count, 
                string.Join(", ", controllerTypes.Select(t => t.Name)));

            // 7. 查找模块初始化器
            var initializerTypes = assembly.GetTypes()
                .Where(t => typeof(IModuleInitializer).IsAssignableFrom(t) && !t.IsAbstract)
                .ToList();

            // 8. 创建插件信息对象
            var pluginInfo = new PluginInfo(
                assembly: assembly,
                loadContext: loadContext,
                plugin: pluginInstance,
                controllerTypes: controllerTypes,
                initializerTypes: initializerTypes,
                pluginPath: pluginPath,
                configuration: configuration);

            _plugins[pluginName] = pluginInfo;

            _logger.LogInformation("插件加载成功: {Name} v{Version}",
                pluginInstance.Name, pluginInstance.Version);

            PluginLoaded?.Invoke(this, new PluginEventArgs(pluginInfo));

            return pluginInfo;
        }
    }

    /// <summary>
    /// 加载插件配置文件
    /// 使用 ConfigurationBuilder 加载插件目录下的 appsettings.json
    /// </summary>
    public static IConfiguration LoadPluginConfiguration(string pluginPath)
    {
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(pluginPath);

        // 加载插件专用配置文件
        var configFile = Path.Combine(pluginPath, "appsettings.json");
        if (File.Exists(configFile))
        {
            configBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        }

        // 加载环境特定配置
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (!string.IsNullOrEmpty(env))
        {
            var envConfigFile = Path.Combine(pluginPath, $"appsettings.{env}.json");
            if (File.Exists(envConfigFile))
            {
                configBuilder.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);
            }
        }

        return configBuilder.Build();
    }

    /// <summary>
    /// 卸载插件
    /// </summary>
    public bool UnloadPlugin(string pluginName)
    {
        lock (_lock)
        {
            if (!_plugins.TryGetValue(pluginName, out var plugin))
            {
                _logger.LogWarning("插件不存在: {PluginName}", pluginName);
                return false;
            }

            _plugins.Remove(pluginName);

            // 卸载程序集上下文
            var loadContext = plugin.LoadContext;
            loadContext.Unload();

            // 触发 GC 清理（确保程序集被回收）
            for (int i = 0; i < 5; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            _logger.LogInformation("插件已卸载: {PluginName}", pluginName);
            PluginUnloaded?.Invoke(this, new PluginEventArgs(plugin));

            return true;
        }
    }

    /// <summary>
    /// 重载插件
    /// </summary>
    public PluginInfo? ReloadPlugin(string pluginName)
    {
        lock (_lock)
        {
            if (!_plugins.TryGetValue(pluginName, out var oldPlugin))
            {
                _logger.LogWarning("插件不存在，无法重载: {PluginName}", pluginName);
                return null;
            }

            var pluginPath = oldPlugin.PluginPath;

            // 卸载旧插件
            UnloadPlugin(pluginName);

            // 等待文件释放
            Thread.Sleep(100);

            // 加载新插件
            var newPlugin = LoadPlugin(pluginPath);

            if (newPlugin != null)
            {
                _logger.LogInformation("插件重载成功: {PluginName}", pluginName);
                PluginReloaded?.Invoke(this, new PluginEventArgs(newPlugin));
            }

            return newPlugin;
        }
    }

    /// <summary>
    /// 卸载所有插件
    /// </summary>
    public void UnloadAll()
    {
        lock (_lock)
        {
            var pluginNames = _plugins.Keys.ToList();
            foreach (var name in pluginNames)
            {
                UnloadPlugin(name);
            }
            _logger.LogInformation("所有插件已卸载");
        }
    }

    /// <summary>
    /// 重载所有插件
    /// </summary>
    public void ReloadAll()
    {
        lock (_lock)
        {
            var pluginNames = _plugins.Keys.ToList();
            foreach (var name in pluginNames)
            {
                ReloadPlugin(name);
            }
            _logger.LogInformation("所有插件已重载");
        }
    }

    /// <summary>
    /// 获取插件
    /// </summary>
    public PluginInfo? GetPlugin(string pluginName)
    {
        lock (_lock)
        {
            return _plugins.TryGetValue(pluginName, out var plugin) ? plugin : null;
        }
    }
}

/// <summary>
/// 插件事件参数
/// </summary>
public class PluginEventArgs : EventArgs
{
    public PluginInfo Plugin { get; }

    public PluginEventArgs(PluginInfo plugin)
    {
        Plugin = plugin;
    }
}
