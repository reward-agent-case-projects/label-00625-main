using PluginSystem.Core;
using PluginSystem.Contracts;

namespace Microsoft.AspNetCore.Builder;

public static class PluginApplicationExtensions
{
    /// <summary>
    /// 使用插件系统
    /// </summary>
    public static IApplicationBuilder UsePlugins(
        this IApplicationBuilder app,
        IEnumerable<PluginInfo> plugins)
    {
        var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
        var logger = app.ApplicationServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("PluginSystem");

        // 按顺序配置插件应用程序
        foreach (var plugin in plugins)
        {
            try
            {
                plugin.Plugin.ConfigureApplication(app, env);
                logger.LogInformation("插件应用程序配置成功: {PluginName}", plugin.Plugin.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "配置插件应用程序失败: {PluginName}", plugin.Plugin.Name);
            }
        }

        return app;
    }

    /// <summary>
    /// 初始化插件模块
    /// </summary>
    public static async Task InitializePluginsAsync(
        this IApplicationBuilder app,
        IEnumerable<PluginInfo> plugins)
    {
        var logger = app.ApplicationServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("PluginSystem");

        // 按 Order 排序初始化器
        var allInitializers = new List<IModuleInitializer>();

        foreach (var plugin in plugins)
        {
            foreach (var initializerType in plugin.InitializerTypes)
            {
                if (ActivatorUtilities.CreateInstance(app.ApplicationServices, initializerType)
                    is IModuleInitializer initializer)
                {
                    allInitializers.Add(initializer);
                }
            }
        }

        allInitializers = allInitializers.OrderBy(i => i.Order).ToList();

        // 执行初始化
        foreach (var initializer in allInitializers)
        {
            try
            {
                await initializer.InitializeAsync(app.ApplicationServices);
                logger.LogInformation("模块初始化成功: {Initializer}", initializer.GetType().Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "插件模块初始化失败: {Initializer}", initializer.GetType().Name);
            }
        }
    }
}
