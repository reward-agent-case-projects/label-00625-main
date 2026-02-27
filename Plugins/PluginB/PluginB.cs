using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PluginSystem.Contracts;

namespace PluginB;

/// <summary>
/// 问候插件 - 提供多语言问候功能
/// </summary>
public class GreetingPlugin : IPlugin
{
    public string Name => "PluginB";
    public string Version => "1.0.0";
    public string Description => "问候插件 - 提供多语言问候功能";
    public string[] Dependencies => Array.Empty<string>();

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 注册问候服务
        services.AddScoped<IGreetingService, GreetingService>();
        
        // 绑定插件配置
        services.Configure<GreetingOptions>(configuration.GetSection("Greeting"));
    }

    public void ConfigureApplication(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var logger = app.ApplicationServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger<GreetingPlugin>();
        
        logger.LogInformation("问候插件已配置");
    }
}
