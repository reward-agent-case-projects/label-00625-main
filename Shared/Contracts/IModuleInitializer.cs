namespace PluginSystem.Contracts;

/// <summary>
/// 模块初始化器接口 - 用于插件启动时执行初始化逻辑
/// </summary>
public interface IModuleInitializer
{
    /// <summary>
    /// 初始化顺序，数值越小越先执行
    /// </summary>
    int Order { get; }

    /// <summary>
    /// 异步初始化方法
    /// </summary>
    /// <param name="serviceProvider">服务提供程序</param>
    Task InitializeAsync(IServiceProvider serviceProvider);
}
