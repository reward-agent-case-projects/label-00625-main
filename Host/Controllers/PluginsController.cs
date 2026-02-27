namespace PluginSystem.Host.Controllers;

/// <summary>
/// 插件管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PluginsController : ControllerBase
{
    private readonly PluginManager _pluginManager;
    private readonly PluginServiceProvider _serviceProvider;
    private readonly PluginOptions _pluginOptions;
    private readonly ILogger<PluginsController> _logger;

    public PluginsController(
        PluginManager pluginManager,
        PluginServiceProvider serviceProvider,
        PluginOptions pluginOptions,
        ILogger<PluginsController> logger)
    {
        _pluginManager = pluginManager;
        _serviceProvider = serviceProvider;
        _pluginOptions = pluginOptions;
        _logger = logger;
    }

    /// <summary>
    /// 获取已加载的插件列表
    /// </summary>
    [HttpGet]
    public ActionResult<IEnumerable<PluginDto>> GetPlugins()
    {
        var plugins = _pluginManager.Plugins.Select(p => new PluginDto
        {
            Name = p.Plugin.Name,
            Version = p.Plugin.Version,
            Description = p.Plugin.Description,
            Dependencies = p.Plugin.Dependencies,
            ControllerCount = p.ControllerTypes.Count,
            Path = p.PluginPath
        });

        return Ok(plugins);
    }

    /// <summary>
    /// 获取插件详情
    /// </summary>
    [HttpGet("{name}")]
    public ActionResult<PluginDetailDto> GetPlugin(string name)
    {
        var plugin = _pluginManager.GetPlugin(name);

        if (plugin == null)
        {
            return NotFound(new { message = $"插件 '{name}' 未找到" });
        }

        var detail = new PluginDetailDto
        {
            Name = plugin.Plugin.Name,
            Version = plugin.Plugin.Version,
            Description = plugin.Plugin.Description,
            Dependencies = plugin.Plugin.Dependencies,
            Path = plugin.PluginPath,
            Controllers = plugin.ControllerTypes.Select(t => new ControllerDto
            {
                Name = t.Name,
                FullName = t.FullName ?? t.Name,
                Actions = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsSpecialName)
                    .Select(m => m.Name)
                    .ToArray()
            }).ToList(),
            Initializers = plugin.InitializerTypes.Select(t => t.Name).ToArray()
        };

        return Ok(detail);
    }

    /// <summary>
    /// 重载指定插件服务（热重载）
    /// </summary>
    [HttpPost("{name}/reload")]
    public ActionResult ReloadPlugin(string name)
    {
        _logger.LogInformation("收到重载插件请求: {PluginName}", name);

        var existingPlugin = _pluginManager.GetPlugin(name);
        if (existingPlugin == null)
        {
            return NotFound(new { message = $"插件 '{name}' 未找到" });
        }

        try
        {
            // 重载插件服务容器（不卸载程序集，因为控制器依赖它）
            var config = LoadPluginConfiguration(existingPlugin.PluginPath);
            _serviceProvider.ReloadPluginServices(name, existingPlugin, config);

            return Ok(new
            {
                message = $"插件 '{name}' 服务热重载成功",
                success = true,
                plugin = new
                {
                    name = existingPlugin.Plugin.Name,
                    version = existingPlugin.Plugin.Version
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重载插件失败: {PluginName}", name);
            return StatusCode(500, new { message = "插件重载失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 重载所有插件服务
    /// </summary>
    [HttpPost("reload")]
    public ActionResult ReloadAllPlugins()
    {
        _logger.LogInformation("收到重载所有插件请求");

        try
        {
            var results = new List<object>();

            foreach (var plugin in _pluginManager.Plugins)
            {
                try
                {
                    var config = LoadPluginConfiguration(plugin.PluginPath);
                    _serviceProvider.ReloadPluginServices(plugin.Plugin.Name, plugin, config);
                    results.Add(new { name = plugin.Plugin.Name, success = true, version = plugin.Plugin.Version });
                }
                catch (Exception ex)
                {
                    results.Add(new { name = plugin.Plugin.Name, success = false, error = ex.Message });
                }
            }

            return Ok(new
            {
                message = "插件服务热重载完成",
                results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重载所有插件失败");
            return StatusCode(500, new { message = "重载失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 卸载指定插件
    /// </summary>
    [HttpPost("{name}/unload")]
    public ActionResult UnloadPlugin(string name)
    {
        _logger.LogInformation("收到卸载插件请求: {PluginName}", name);

        var plugin = _pluginManager.GetPlugin(name);
        if (plugin == null)
        {
            return NotFound(new { message = $"插件 '{name}' 未找到" });
        }

        try
        {
            _serviceProvider.UnloadPlugin(name);
            _pluginManager.UnloadPlugin(name);

            return Ok(new { message = $"插件 '{name}' 已卸载", success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "卸载插件失败: {PluginName}", name);
            return StatusCode(500, new { message = "卸载失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 卸载所有插件
    /// </summary>
    [HttpPost("unload")]
    public ActionResult UnloadAllPlugins()
    {
        _logger.LogInformation("收到卸载所有插件请求");

        try
        {
            var pluginNames = _pluginManager.Plugins.Select(p => p.Plugin.Name).ToList();
            
            foreach (var name in pluginNames)
            {
                _serviceProvider.UnloadPlugin(name);
            }
            
            _pluginManager.UnloadAll();

            return Ok(new { message = "所有插件已卸载", success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "卸载所有插件失败");
            return StatusCode(500, new { message = "卸载失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取插件系统状态
    /// </summary>
    [HttpGet("status")]
    public ActionResult<PluginSystemStatus> GetStatus()
    {
        return Ok(new PluginSystemStatus
        {
            PluginPath = _pluginManager.PluginsPath,
            HotReloadEnabled = _pluginOptions.EnableHotReload,
            HotReloadDelay = _pluginOptions.HotReloadDelay,
            LoadedPluginCount = _pluginManager.Plugins.Count,
            Plugins = _pluginManager.Plugins.Select(p => new PluginStatusDto
            {
                Name = p.Plugin.Name,
                Version = p.Plugin.Version,
                IsLoaded = true
            }).ToList()
        });
    }

    private IConfiguration LoadPluginConfiguration(string pluginPath)
    {
        var configBuilder = new ConfigurationBuilder();
        var configFile = Path.Combine(pluginPath, "appsettings.json");
        if (System.IO.File.Exists(configFile))
        {
            configBuilder.AddJsonFile(configFile, optional: true, reloadOnChange: true);
        }
        return configBuilder.Build();
    }
}

public class PluginDto
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] Dependencies { get; set; } = Array.Empty<string>();
    public int ControllerCount { get; set; }
    public string Path { get; set; } = string.Empty;
}

public class PluginDetailDto
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] Dependencies { get; set; } = Array.Empty<string>();
    public string Path { get; set; } = string.Empty;
    public List<ControllerDto> Controllers { get; set; } = new();
    public string[] Initializers { get; set; } = Array.Empty<string>();
}

public class ControllerDto
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string[] Actions { get; set; } = Array.Empty<string>();
}

public class PluginSystemStatus
{
    public string PluginPath { get; set; } = string.Empty;
    public bool HotReloadEnabled { get; set; }
    public int HotReloadDelay { get; set; }
    public int LoadedPluginCount { get; set; }
    public List<PluginStatusDto> Plugins { get; set; } = new();
}

public class PluginStatusDto
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsLoaded { get; set; }
}
