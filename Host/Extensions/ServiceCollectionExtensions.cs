using PluginSystem.Core;
using PluginSystem.Contracts;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 插件系统服务扩展
/// </summary>
public static class PluginServiceExtensions
{
    /// <summary>
    /// 添加插件系统核心服务
    /// </summary>
    public static IServiceCollection AddPluginSystem(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 注册插件管理器
        services.AddSingleton<PluginManager>();

        // 注册插件配置
        var pluginOptions = new PluginOptions();
        configuration.GetSection("Plugins").Bind(pluginOptions);
        services.AddSingleton(pluginOptions);

        return services;
    }

    /// <summary>
    /// 配置 MVC 支持插件控制器
    /// 使用 AddApplicationPart() 将插件程序集添加到 MVC 应用程序部分
    /// </summary>
    public static IMvcBuilder AddPluginMvc(
        this IServiceCollection services,
        IEnumerable<PluginInfo> plugins)
    {
        var mvcBuilder = services.AddControllers();

        var pluginList = plugins.ToList();
        if (pluginList.Any())
        {
            mvcBuilder.ConfigureApplicationPartManager(manager =>
            {
                foreach (var plugin in pluginList)
                {
                    // 关键：使用 AddApplicationPart 注册插件程序集
                    // 这样 ASP.NET Core 才能发现并路由到插件中的控制器
                    var assemblyPart = new PluginAssemblyPart(plugin.Assembly);
                    manager.ApplicationParts.Add(assemblyPart);
                }
            });
        }

        return mvcBuilder;
    }

    /// <summary>
    /// 注册插件服务访问器
    /// 控制器通过 IPluginServiceAccessor 动态获取插件服务，实现真正的热重载
    /// </summary>
    public static IServiceCollection AddPluginServiceAccessor(
        this IServiceCollection services,
        PluginServiceProvider pluginServiceProvider)
    {
        // 注册 IPluginServiceAccessor，控制器通过它动态获取插件服务
        // 插件服务在独立容器中管理，不注册到主容器，支持热重载
        services.AddSingleton<IPluginServiceAccessor>(pluginServiceProvider);
        
        return services;
    }

    /// <summary>
    /// 注册插件服务到独立容器并配置依赖注入
    /// </summary>
    public static void RegisterPluginServices(
        this PluginServiceProvider serviceProvider,
        IEnumerable<PluginInfo> plugins,
        ILogger logger)
    {
        foreach (var plugin in plugins)
        {
            try
            {
                // 使用插件自带的配置
                serviceProvider.RegisterPlugin(plugin, plugin.Configuration);
                
                logger.LogInformation("插件服务注册成功: {PluginName}", plugin.Plugin.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "注册插件服务失败: {PluginName}", plugin.Plugin.Name);
            }
        }
    }
}
