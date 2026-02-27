using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PluginSystem.Contracts;

namespace PluginA;

/// <summary>
/// 计算器插件 - 提供基础数学运算功能
/// </summary>
public class CalculatorPlugin : IPlugin
{
    public string Name => "PluginA";
    public string Version => "1.0.0";
    public string Description => "计算器插件 - 提供加减乘除等数学运算";
    public string[] Dependencies => Array.Empty<string>();

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 注册计算器服务
        services.AddScoped<ICalculatorService, CalculatorService>();
        
        // 绑定插件配置
        services.Configure<CalculatorOptions>(configuration.GetSection("Calculator"));
    }

    public void ConfigureApplication(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var logger = app.ApplicationServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger<CalculatorPlugin>();
        
        logger.LogInformation("计算器插件已配置");
    }
}
