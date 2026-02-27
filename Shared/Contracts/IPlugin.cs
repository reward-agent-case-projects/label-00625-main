namespace PluginSystem.Contracts;

/// <summary>
/// 插件接口 - 所有插件必须实现此接口
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// 插件名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 插件版本
    /// </summary>
    string Version { get; }

    /// <summary>
    /// 插件描述
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 插件依赖的其他插件名称
    /// </summary>
    string[] Dependencies { get; }

    /// <summary>
    /// 配置服务 - 在此注册插件所需的服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">插件配置</param>
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>
    /// 配置应用程序 - 在此配置中间件等
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <param name="env">Web主机环境</param>
    void ConfigureApplication(IApplicationBuilder app, IWebHostEnvironment env);
}
